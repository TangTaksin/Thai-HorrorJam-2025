using UnityEngine;

public class SignPainter : MonoBehaviour
{
    [Header("Brush Settings")]
    public int brushSize = 10; // ขนาดแปรง (pixel)
    public Color paintColor = Color.black; // สีที่จะวาด

    private Camera playerCamera;
    private Texture2D drawableTexture;
    private bool textureNeedsUpdate = false; // Flag สำหรับ Optimize

    void Start()
    {
        playerCamera = Camera.main; // หากล้องหลัก

        // --- ส่วนสำคัญ: สร้าง Texture ใหม่ขึ้นมาใน Memory ---
        // เราไม่สามารถแก้ไข Asset Texture โดยตรงได้
        // เราต้องสร้าง "สำเนา" ของมันขึ้นมาตอนเริ่มเกม

        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("SignPainter: ไม่พบ Renderer บน Object นี้");
            this.enabled = false;
            return;
        }

        // 1. ดึง Texture เดิมจาก Material
        Texture originalTexture = rend.material.mainTexture;
        if (originalTexture == null)
        {
            Debug.LogError("SignPainter: Material ไม่มี Main Texture (Base Map)");
            this.enabled = false;
            return;
        }

        // 2. สร้าง Texture2D ใหม่ที่ "เขียนได้" (writable)
        drawableTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);

        // 3. คัดลอก Texture เดิม (ที่เป็นแค่ Read-only) มาใส่ใน Texture ใหม่
        // เราต้องใช้ Graphics.CopyTexture เพราะ Read/Write Texture มันยุ่งยาก
        // วิธีที่ง่ายกว่าคือการ Render Texture เดิมลงไป
        RenderTexture rt = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height);
        Graphics.Blit(originalTexture, rt);
        RenderTexture.active = rt;
        drawableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        drawableTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);


        // 4. สั่งให้ Material ใช้ Texture ใหม่ที่เราสร้างขึ้นมาแทน
        rend.material.mainTexture = drawableTexture;
    }

    void Update()
    {
        // ตรวจสอบว่าผู้เล่น "กดเมาส์ซ้าย" ค้างไว้หรือไม่
        if (Input.GetMouseButton(0))
        {
            // ยิง Raycast จากตำแหน่งเมาส์
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // ตรวจสอบว่า Raycast ชนกับ Object ที่มี Script นี้อยู่หรือไม่
                // (ต้องมั่นใจว่า Object นี้มี Mesh Collider)
                if (hit.collider == GetComponent<Collider>())
                {
                    // ถ้าใช่, เรียกฟังก์ชันวาด
                    DrawOnTexture(hit.textureCoord);
                }
            }
        }
    }

    // ฟังก์ชันนี้จะถูกเรียกใน LateUpdate เพื่อ Apply แค่ครั้งเดียวต่อเฟรม
    void LateUpdate()
    {
        if (textureNeedsUpdate)
        {
            // Apply การเปลี่ยนแปลงทั้งหมดไปยัง GPU
            drawableTexture.Apply();
            textureNeedsUpdate = false;
        }
    }

    // ฟังก์ชันสำหรับให้ Script อื่นมาดึง Texture ที่วาดได้
    public Texture2D GetDrawableTexture()
    {
        return drawableTexture;
    }



    // นี่คือหัวใจหลักของการวาด
    // (สำคัญ!) เปลี่ยนจาก private (ไม่มีอะไรนำหน้า) เป็น public
    // เพื่อให้ ShowDrawing เรียกใช้ได้
    public void DrawOnTexture(Vector2 uvCoordinate)
    {
        if (drawableTexture == null) return; // (เพิ่ม) เช็กเผื่อไว้

        int pixelX = (int)(uvCoordinate.x * drawableTexture.width);
        int pixelY = (int)(uvCoordinate.y * drawableTexture.height);
        for (int y = -brushSize / 2; y <= brushSize / 2; y++)
        {
            for (int x = -brushSize / 2; x <= brushSize / 2; x++)
            {
                if (new Vector2(x, y).magnitude > brushSize / 2) continue;
                int targetX = pixelX + x;
                int targetY = pixelY + y;
                if (targetX >= 0 && targetX < drawableTexture.width &&
                    targetY >= 0 && targetY < drawableTexture.height)
                {
                    drawableTexture.SetPixel(targetX, targetY, paintColor);
                }
            }
        }
        textureNeedsUpdate = true;
    }
}
