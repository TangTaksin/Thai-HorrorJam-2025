using UnityEngine;

public class TextureEraser : MonoBehaviour, IInteractable
{
    [Header("Brush Settings")]
    public BrushType brushType = BrushType.Circle;
    public int brushSize = 20;

    [Header("Spray Settings")]
    [Range(0.1f, 1f)]
    public float sprayDensity = 0.5f;

    [Header("Soft Brush Settings")]
    [Range(0.1f, 3f)]
    public float softnessFactor = 1.5f;

    private Texture2D drawableTexture;
    private bool textureNeedsUpdate = false;
    private Vector2 lastDrawUV;
    private bool isDrawingLastFrame = false;
    private Material drawableMaterial;
    private System.Random sprayRandom;

    // เราไม่ต้องการ paintColor อีกต่อไป
    // public Color paintColor = Color.black;

    void Start()
    {
        sprayRandom = new System.Random();
        InitializeTexture();
    }

    void LateUpdate()
    {
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
            Debug.LogError("TextureEraser: No Renderer found.");
            this.enabled = false;
            return;
        }

        drawableMaterial = rend.material;
        Texture originalTexture = drawableMaterial.GetTexture("_MainTex"); 
        if (originalTexture == null)
        {
            Debug.LogError("TextureEraser: Material does not have a _MainTex texture.");
            this.enabled = false;
            return;
        }

        // --- นี่คือโค้ดดั้งเดิมที่ใช้ ReadPixels ---
        drawableTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
        RenderTexture rt = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height);
        
        Graphics.Blit(originalTexture, rt);
        
        RenderTexture.active = rt;
        drawableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        drawableTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        // ------------------------------------

        drawableMaterial.SetTexture("_MainTex", drawableTexture);
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

    public void Interact(GameObject interactor) { }

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

        // ปรับ StepSize ให้น้อยลงเล็กน้อยเพื่อการลบที่เนียนขึ้น
        float stepSize = Mathf.Max(1.0f, brushRadius * 0.15f);
        int steps = (int)Mathf.Ceil(distance / stepSize);
        steps = Mathf.Max(1, steps);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 interpolatedUV = Vector2.Lerp(startUV, endUV, t);
            DrawOnTexture(interpolatedUV);
        }
    }

    //
    // --- !! นี่คือส่วนที่เปลี่ยนแปลง !! ---
    //
    public void DrawOnTexture(Vector2 uvCoordinate)
    {
        if (drawableTexture == null) return;

        int pixelX = (int)(uvCoordinate.x * drawableTexture.width);
        int pixelY = (int)(uvCoordinate.y * drawableTexture.height);

        float brushRadius = brushSize / 2.0f;
        int intRadius = (int)Mathf.Ceil(brushRadius);

        // --- Optimization: ใช้ GetPixels/SetPixels ---

        // 1. คำนวณขอบเขตของ Brush
        int minX = Mathf.Clamp(pixelX - intRadius, 0, drawableTexture.width - 1);
        int minY = Mathf.Clamp(pixelY - intRadius, 0, drawableTexture.height - 1);
        int maxX = Mathf.Clamp(pixelX + intRadius, 0, drawableTexture.width - 1);
        int maxY = Mathf.Clamp(pixelY + intRadius, 0, drawableTexture.height - 1);

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        // 2. ดึง Pixel ทั้งหมดในขอบเขตมาเก็บใน Array
        Color[] pixelBlock = drawableTexture.GetPixels(minX, minY, width, height);

        // 3. วนลูปใน Array (เร็วกว่า GetPixel/SetPixel ทีละจุดมาก)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // คำนวณตำแหน่งเทียบกับจุดศูนย์กลาง Brush
                int relativeX = (minX + x) - pixelX;
                int relativeY = (minY + y) - pixelY;

                float strength = CalculateBrushStrength(relativeX, relativeY, brushRadius);

                if (strength <= 0) continue;

                // --- นี่คือ Logic การลบ ---
                int index = y * width + x;
                Color originalColor = pixelBlock[index];

                // 1. คำนวณ Alpha ใหม่: เคลื่อนที่จาก Alpha เดิม ไปหา 0 (โปร่งใส)
                // ยิ่ง strength มาก (กลางแปรง) ยิ่งเข้าใกล้ 0 เร็ว
                float newAlpha = Mathf.Lerp(originalColor.a, 0.0f, strength);

                // 2. สร้างสีใหม่: ใช้ R,G,B เดิม แต่ใช้ Alpha ใหม่
                Color newColor = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);

                // 3. อัปเดตสีใน Array
                pixelBlock[index] = newColor;
            }
        }

        // 4. ส่ง Array ทั้งหมดกลับเข้าไปใน Texture ทีเดียว
        drawableTexture.SetPixels(minX, minY, width, height, pixelBlock);

        textureNeedsUpdate = true;
    }

    // ฟังก์ชัน CalculateBrushStrength ทั้งหมดเหมือนเดิม (ไม่จำเป็นต้องเปลี่ยน)
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

    // Brush Logic ทั้งหมดเหมือนเดิม...
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
        if (distance < brushRadius - edgeSize) return 1.0f;
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

    // Public setters (เหมือนเดิม)
    public void SetBrushType(BrushType type) { brushType = type; }
    public void SetBrushSize(int size) { brushSize = Mathf.Max(1, size); }
    public void SetSprayDensity(float density) { sprayDensity = Mathf.Clamp01(density); }
    public void SetSoftnessFactor(float softness) { softnessFactor = Mathf.Clamp(softness, 0.1f, 3f); }
}
