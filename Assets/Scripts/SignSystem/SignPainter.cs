using UnityEngine;
using System.IO;

public class SignPainter : MonoBehaviour, IInteractable
{
    [Header("Brush Settings")]
    public BrushType brushType = BrushType.Circle;
    public int brushSize = 10;
    public Color paintColor = Color.black;

    [Header("Spray Settings")]
    [Range(0.1f, 1f)]
    [Tooltip("ความหนาแน่นของ Spray (0.1 = เบา, 1 = หนา)")]
    public float sprayDensity = 0.5f;

    [Header("Soft Brush Settings")]
    [Range(0.1f, 3f)]
    [Tooltip("ความนุ่มของแปรง (ยิ่งสูง ยิ่งนุ่ม)")]
    public float softnessFactor = 1.5f;

    // --- เพิ่มเข้ามาใหม่ ---
    [Header("Save Settings")]
    [Tooltip("เปิดใช้งานการบันทึกด้วยปุ่มขณะรันเกม")]
    public bool allowRuntimeSave = true;
    [Tooltip("ปุ่มที่ใช้ในการบันทึก Texture")]
    public KeyCode saveKey = KeyCode.LeftControl;
    // ----------------------

    private Texture2D drawableTexture;
    private bool textureNeedsUpdate = false;
    private Vector2 lastDrawUV;
    private bool isDrawingLastFrame = false;
    private Material drawableMaterial;
    private System.Random sprayRandom;

    void Start()
    {
        sprayRandom = new System.Random();
        InitializeTexture();
    }

    void LateUpdate()
    {
        // --- เพิ่มเข้ามาใหม่ ---
        // ตรวจสอบการกดปุ่มเพื่อบันทึก
        if (allowRuntimeSave && Input.GetKeyDown(saveKey))
        {
            SaveTextureToPNG();
        }
        // ----------------------

        if (textureNeedsUpdate)
        {
            drawableTexture.Apply();
            textureNeedsUpdate = false;
        }
    }

    private void InitializeTexture()
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
                int targetX = pixelX + x;
                int targetY = pixelY + y;

                if (targetX < 0 || targetX >= drawableTexture.width ||
                    targetY < 0 || targetY >= drawableTexture.height)
                    continue;

                float strength = CalculateBrushStrength(x, y, brushRadius);

                if (strength <= 0) continue;

                Color originalColor = drawableTexture.GetPixel(targetX, targetY);
                float blendAlpha = strength * paintColor.a;
                Color blendedColor = Color.Lerp(originalColor, paintColor, blendAlpha);

                drawableTexture.SetPixel(targetX, targetY, blendedColor);
            }
        }

        textureNeedsUpdate = true;
    }

    private float CalculateBrushStrength(int x, int y, float brushRadius)
    {
        float distance = new Vector2(x, y).magnitude;

        switch (brushType)
        {
            case BrushType.Circle:
                return CalculateCircleBrush(distance, brushRadius);

            case BrushType.Square:
                return CalculateSquareBrush(x, y, brushRadius);

            case BrushType.Soft:
                return CalculateSoftBrush(distance, brushRadius);

            case BrushType.Hard:
                return CalculateHardBrush(distance, brushRadius);

            case BrushType.Spray:
                return CalculateSprayBrush(distance, brushRadius);

            default:
                return CalculateCircleBrush(distance, brushRadius);
        }
    }

    private float CalculateCircleBrush(float distance, float brushRadius)
    {
        if (distance > brushRadius) return 0f;

        float falloff = distance / brushRadius;
        return Mathf.SmoothStep(1.0f, 0.0f, falloff);
    }

    private float CalculateSquareBrush(int x, int y, float brushRadius)
    {
        float maxDist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
        if (maxDist > brushRadius) return 0f;

        float falloff = maxDist / brushRadius;
        return Mathf.SmoothStep(1.0f, 0.0f, falloff);
    }

    private float CalculateSoftBrush(float distance, float brushRadius)
    {
        if (distance > brushRadius) return 0f;

        float normalizedDist = distance / brushRadius;
        float strength = Mathf.Pow(1.0f - normalizedDist, softnessFactor);
        return Mathf.Clamp01(strength);
    }

    private float CalculateHardBrush(float distance, float brushRadius)
    {
        if (distance > brushRadius) return 0f;

        float edgeSize = brushRadius * 0.2f;
        if (distance < brushRadius - edgeSize)
            return 1.0f;

        float edgeFalloff = (brushRadius - distance) / edgeSize;
        return Mathf.Clamp01(edgeFalloff);
    }

    private float CalculateSprayBrush(float distance, float brushRadius)
    {
        if (distance > brushRadius) return 0f;

        float randomValue = (float)sprayRandom.NextDouble();
        if (randomValue > sprayDensity) return 0f;

        float falloff = distance / brushRadius;
        float baseStrength = 1.0f - falloff;
        return baseStrength * randomValue * 0.5f;
    }

    // ฟังก์ชันเพิ่มเติมสำหรับเปลี่ยน Brush แบบ Runtime
    public void SetBrushType(BrushType type)
    {
        brushType = type;
    }

    public void SetBrushSize(int size)
    {
        brushSize = Mathf.Max(1, size);
    }

    public void SetPaintColor(Color color)
    {
        paintColor = color;
    }

    public void SetSprayDensity(float density)
    {
        sprayDensity = Mathf.Clamp01(density);
    }

    public void SetSoftnessFactor(float softness)
    {
        softnessFactor = Mathf.Clamp(softness, 0.1f, 3f);
    }

    /// <summary>
    /// เพิ่มเมนูคลิกขวาใน Inspector เพื่อบันทึก Texture
    /// </summary>
    [ContextMenu("Save Texture as PNG")]
    public void SaveTextureToPNG()
    {
        if (drawableTexture == null)
        {
            Debug.LogError("SignPainter: ไม่มี Texture ให้บันทึก!");
            return;
        }

        // ตรวจสอบว่ามีการอัปเดตที่ค้างอยู่หรือไม่
        if (textureNeedsUpdate)
        {
            drawableTexture.Apply();
            textureNeedsUpdate = false;
        }

        // แปลง Texture2D เป็น mbyte array (PNG)
        byte[] pngData = drawableTexture.EncodeToPNG();
        if (pngData == null)
        {
            Debug.LogError("SignPainter: ไม่สามารถ Encode texture เป็น PNG ได้");
            return;
        }

        // สร้างชื่อไฟล์ที่ไม่ซ้ำกัน
        string fileName = string.Format("PaintedSign_{0}.png", System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        
        // กำหนด path ให้อยู่ในโฟลเดอร์ Assets
        string path = Path.Combine(Application.dataPath, fileName);

        // เขียนไฟล์
        try
        {
            File.WriteAllBytes(path, pngData);
            Debug.Log($"<color=green>บันทึก Texture สำเร็จ:</color> {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SignPainter: ไม่สามารถบันทึกไฟล์ได้: {e.Message}");
            return;
        }

        // สั่งให้ AssetDatabase รีเฟรช เพื่อให้ไฟล์ใหม่แสดงใน Unity Editor
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }
}