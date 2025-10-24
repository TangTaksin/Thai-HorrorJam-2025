using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

public class PaintManager_WebGL : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Texture2D brushTexture;
    public LayerMask paintableLayer;

    [Header("Brush Settings")]
    public Color brushColor = Color.red;
    [Range(0.01f, 0.5f)] public float brushSize = 0.05f;
    [Range(2, 20)] public int strokeSteps = 8;

    [Header("Undo/Redo Settings")]
    public int maxUndoSteps = 10;

    private Material paintMaterial;
    private Vector2? lastUV = null;

    // Renderer -> RenderTexture per object
    private Dictionary<Renderer, RenderTexture> rendererRTs = new();
    private Dictionary<Renderer, Stack<RenderTexture>> undoStacks = new();
    private Dictionary<Renderer, Stack<RenderTexture>> redoStacks = new();

    void Start()
    {
        // --- Load shader safely (works in WebGL) ---
        Shader paintShader = Resources.Load<Shader>("UnlitPaint");
        if (paintShader == null)
        {
            Debug.LogError("❌ Shader not found! Put 'UnlitPaint.shader' in Assets/Resources/");
            enabled = false;
            return;
        }

        paintMaterial = new Material(paintShader);
        paintMaterial.SetTexture("_BrushTex", brushTexture);
        paintMaterial.SetColor("_BrushColor", brushColor);

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("❌ No camera assigned and no MainCamera found!");
                enabled = false;
                return;
            }
        }
    }

    void Update()
    {
        HandlePainting();
        HandleUndoRedo();
    }

    private void HandlePainting()
    {
        if (!Input.GetMouseButton(0))
        {
            lastUV = null;
            return;
        }

        // Ray from camera center (use viewport for FPS-like painting)
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, 50f, paintableLayer))
            return;

        Renderer rend = hit.collider.GetComponent<Renderer>();
        if (rend == null || rend.material == null)
            return;

        // Create or get RenderTexture per object
        if (!rendererRTs.TryGetValue(rend, out RenderTexture rt) || rt == null)
        {
            int width = 1024;
            int height = 1024;
            rt = new RenderTexture(width, height, 0)
            {
                wrapMode = TextureWrapMode.Repeat,
                graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm
            };
            rt.Create();

            Texture2D baseTex = rend.material.mainTexture as Texture2D;
            if (baseTex != null)
                Graphics.Blit(baseTex, rt);
            else
                Graphics.Blit(Texture2D.whiteTexture, rt);

            rend.material.SetTexture("_MainTex", rt);
            rendererRTs[rend] = rt;
            undoStacks[rend] = new Stack<RenderTexture>();
            redoStacks[rend] = new Stack<RenderTexture>();
        }

        if (paintMaterial == null) return;

        paintMaterial.SetColor("_BrushColor", brushColor);
        Vector2 uv = hit.textureCoord;

        if (!lastUV.HasValue)
            SaveUndo(rend, rt);

        Vector2 startUV = lastUV ?? uv;

        // Temporary RT (one per stroke)
        RenderTexture temp = RenderTexture.GetTemporary(rt.descriptor);

        for (int i = 0; i <= strokeSteps; i++)
        {
            Vector2 lerpUV = Vector2.Lerp(startUV, uv, i / (float)strokeSteps);
            paintMaterial.SetVector("_UVPos", new Vector4(lerpUV.x, lerpUV.y, brushSize, 0));

            Graphics.Blit(rt, temp);
            Graphics.Blit(temp, rt, paintMaterial);
        }

        RenderTexture.ReleaseTemporary(temp);
        lastUV = uv;
    }

    private void SaveUndo(Renderer rend, RenderTexture rt)
    {
        if (!undoStacks.ContainsKey(rend)) return;

        RenderTexture snapshot = new RenderTexture(rt.width, rt.height, 0)
        {
            graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm
        };
        snapshot.Create();
        Graphics.Blit(rt, snapshot);

        undoStacks[rend].Push(snapshot);
        while (undoStacks[rend].Count > maxUndoSteps)
        {
            RenderTexture old = undoStacks[rend].ToArray()[0];
            if (old != null) old.Release();
            undoStacks[rend].Pop();
        }

        redoStacks[rend].Clear();
    }

    private void HandleUndoRedo()
    {
        if (Input.GetKeyDown(KeyCode.Z)) Undo();
        if (Input.GetKeyDown(KeyCode.Y)) Redo();
    }

    private void Undo()
    {
        foreach (var kv in undoStacks)
        {
            Renderer rend = kv.Key;
            if (kv.Value.Count == 0) continue;

            RenderTexture current = rendererRTs[rend];
            RenderTexture last = kv.Value.Pop();

            redoStacks[rend].Push(current);
            rendererRTs[rend] = last;
            rend.material.SetTexture("_MainTex", last);
        }
    }

    private void Redo()
    {
        foreach (var kv in redoStacks)
        {
            Renderer rend = kv.Key;
            if (kv.Value.Count == 0) continue;

            RenderTexture current = rendererRTs[rend];
            RenderTexture next = kv.Value.Pop();

            undoStacks[rend].Push(current);
            rendererRTs[rend] = next;
            rend.material.SetTexture("_MainTex", next);
        }
    }
}
