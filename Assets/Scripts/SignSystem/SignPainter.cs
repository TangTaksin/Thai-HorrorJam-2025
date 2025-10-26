using UnityEngine;

public class SignPainter : MonoBehaviour, IInteractable
{
    [Header("Brush Settings")]
    public int brushSize = 10;
    public Color paintColor = Color.black;
    private Texture2D drawableTexture;
    private bool textureNeedsUpdate = false;
    private Vector2 lastDrawUV;
    private bool isDrawingLastFrame = false;
    private Material drawableMaterial;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("SignPainter: No Renderer found on this object.");
            this.enabled = false;
            return;
        }

        drawableMaterial = rend.material;
        Texture originalTexture = drawableMaterial.mainTexture;
        if (originalTexture == null)
        {
            Debug.LogError("SignPainter: Material does not have a Main Texture (Base Map).");
            this.enabled = false;
            return;
        }

        drawableTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
        RenderTexture rt = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height);
        Graphics.Blit(originalTexture, rt);
        RenderTexture.active = rt;
        drawableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        drawableTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        drawableMaterial.mainTexture = drawableTexture;
    }

    void LateUpdate()
    {
        if (textureNeedsUpdate)
        {
            drawableTexture.Apply();
            textureNeedsUpdate = false;
        }
    }

    public void ExternalPaint(Vector2 rawUV)
    {
        Vector2 tiling = drawableMaterial.mainTextureScale;
        Vector2 offset = drawableMaterial.mainTextureOffset;

        Vector2 transformedUV = (rawUV * tiling) + offset;
        transformedUV.x = transformedUV.x - Mathf.Floor(transformedUV.x);
        transformedUV.y = transformedUV.y - Mathf.Floor(transformedUV.y);

        PaintStroke(transformedUV);
    }

    public void StopPainting()
    {
        isDrawingLastFrame = false;
    }

    public void Interact(GameObject interactor)
    {
    }

    public Texture2D GetDrawableTexture()
    {
        return drawableTexture;
    }

    private void PaintStroke(Vector2 currentUV)
    {
        if (!isDrawingLastFrame)
        {
            DrawOnTexture(currentUV);
        }
        else
        {
            InterpolateDraw(lastDrawUV, currentUV);
        }

        lastDrawUV = currentUV;
        isDrawingLastFrame = true;
    }

    private void InterpolateDraw(Vector2 startUV, Vector2 endUV)
    {
        Vector2 startPixel = new Vector2(startUV.x * drawableTexture.width, startUV.y * drawableTexture.height);
        Vector2 endPixel = new Vector2(endUV.x * drawableTexture.width, endUV.y * drawableTexture.height);

        float distance = Vector2.Distance(startPixel, endPixel);
        float brushRadius = brushSize / 2.0f;

        float stepSize = Mathf.Max(1.0f, brushRadius * 0.25f);
        int steps = (int)Mathf.Ceil(distance / stepSize);
        steps = Mathf.Max(1, steps);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 interpolatedUV = Vector2.Lerp(startUV, endUV, t);
            DrawOnTexture(interpolatedUV);
        }
    }

    public void DrawOnTexture(Vector2 uvCoordinate)
    {
        if (drawableTexture == null) return;

        int pixelX = (int)(uvCoordinate.x * drawableTexture.width);
        int pixelY = (int)(uvCoordinate.y * drawableTexture.height);

        float brushRadius = brushSize / 2.0f;
        int intRadius = (int)Mathf.Ceil(brushRadius);

        for (int y = -intRadius; y <= intRadius; y++)
        {
            for (int x = -intRadius; x <= intRadius; x++)
            {
                float distance = new Vector2(x, y).magnitude;
                if (distance > brushRadius) continue;

                int targetX = pixelX + x;
                int targetY = pixelY + y;

                if (targetX >= 0 && targetX < drawableTexture.width && targetY >= 0 && targetY < drawableTexture.height)
                {
                    float falloff = distance / brushRadius;
                    float strength = Mathf.SmoothStep(1.0f, 0.0f, falloff);

                    Color originalColor = drawableTexture.GetPixel(targetX, targetY);
                    float blendAlpha = strength * paintColor.a;
                    Color blendedColor = Color.Lerp(originalColor, paintColor, blendAlpha);

                    drawableTexture.SetPixel(targetX, targetY, blendedColor);
                }
            }
        }

        textureNeedsUpdate = true;
    }

}
