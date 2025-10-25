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

    // นี่คือหัวใจหลักของการวาด
    void DrawOnTexture(Vector2 uvCoordinate)
    {
        // 1. แปลง UV (0.0 - 1.0) เป็นพิกัด Pixel (เช่น 0 - 512)
        int pixelX = (int)(uvCoordinate.x * drawableTexture.width);
        int pixelY = (int)(uvCoordinate.y * drawableTexture.height);

        // 2. วนลูปเป็นสี่เหลี่ยมรอบจุดที่ชน เพื่อสร้าง "ขนาดแปรง"
        for (int y = -brushSize / 2; y <= brushSize / 2; y++)
        {
            for (int x = -brushSize / 2; x <= brushSize / 2; x++)
            {
                // (Optional) ทำให้แปรงเป็นวงกลมแทนสี่เหลี่ยม
                if (new Vector2(x, y).magnitude > brushSize / 2)
                {
                    continue; // ข้าม pixel ที่อยู่นอกวงกลม
                }

                int targetX = pixelX + x;
                int targetY = pixelY + y;

                // 3. ตรวจสอบว่าพิกัดไม่ออกนอกขอบ Texture
                if (targetX >= 0 && targetX < drawableTexture.width &&
                    targetY >= 0 && targetY < drawableTexture.height)
                {
                    // 4. ตั้งค่าสี Pixel
                    drawableTexture.SetPixel(targetX, targetY, paintColor);
                }
            }
        }

        // 5. ตั้งค่า Flag ว่า Texture นี้ต้องอัปเดต
        // เราจะไม่ Apply() ที่นี่ เพราะมันช้ามากถ้าทำทุก Pixel
        textureNeedsUpdate = true;
    }
}
