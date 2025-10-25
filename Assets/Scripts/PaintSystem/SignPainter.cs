using UnityEngine;

public class SignPainter : MonoBehaviour
{
    [Header("Brush Settings")]
    public int brushSize = 10; // ขนาดแปรง (pixel)
    public Color paintColor = Color.black; // สีที่จะวาด

    private Camera playerCamera;
    private Texture2D drawableTexture;
    private bool textureNeedsUpdate = false; // Flag สำหรับ Optimize

    private Vector2 lastDrawUV; // เก็บตำแหน่ง UV ที่วาดในเฟรมก่อนหน้า
    private bool isDrawingLastFrame = false; // เก็บสถานะว่าเฟรมที่แล้วกำลังวาดอยู่หรือไม่

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
                if (hit.collider == GetComponent<Collider>())
                {
                    // --- [เปลี่ยนตรงนี้] ---
                    // ถ้าใช่, เรียกฟังก์ชันวาดแบบ "ลากเส้น"
                    Vector2 currentDrawUV = hit.textureCoord;
                    PaintStroke(currentDrawUV);
                    // --- [จบส่วนที่เปลี่ยน] ---
                }
                else
                {
                    // ถ้าเมาส์กดค้าง แต่ออกนอกป้าย ให้หยุดการลากเส้น
                    isDrawingLastFrame = false;
                }
            }
            else
            {
                // ถ้าเมาส์กดค้าง แต่ไม่โดนอะไรเลย ให้หยุดการลากเส้น
                isDrawingLastFrame = false;
            }
        }
        // ถ้าปล่อยเมาส์
        else if (Input.GetMouseButtonUp(0))
        {
            isDrawingLastFrame = false;
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

    // (ฟังก์ชันนี้เป็นฟังก์ชันใหม่ที่เพิ่มเข้ามา)
    // ทำหน้าที่วาด "เส้น" เชื่อมระหว่างจุดเก่ากับจุดใหม่
    void PaintStroke(Vector2 currentUV)
    {
        // ถ้าเฟรมนี้เป็นเฟรมแรกที่เพิ่งเริ่มกด (ยังไม่มีจุดเก่า)
        // ให้วาดแค่จุดเดียวพอ
        if (!isDrawingLastFrame)
        {
            DrawOnTexture(currentUV);
        }
        // ถ้ากำลังวาดค้างอยู่ (มีจุดเก่าจากเฟรมที่แล้ว)
        else
        {
            // เรียกฟังก์ชันวาดแบบถมช่องว่าง
            InterpolateDraw(lastDrawUV, currentUV);
        }

        // บันทึกตำแหน่งปัจจุบันไว้เป็น "จุดเก่า" สำหรับเฟรมหน้า
        lastDrawUV = currentUV;
        isDrawingLastFrame = true;
    }

    // (ฟังก์ชันนี้เป็นฟังก์ชันใหม่ที่เพิ่มเข้ามา)
    // วาดจุดเชื่อมต่อระหว่าง 2 UVs
    void InterpolateDraw(Vector2 startUV, Vector2 endUV)
    {
        // 1. แปลง UV (0-1) เป็น Pixel (0 - width)
        Vector2 startPixel = new Vector2(startUV.x * drawableTexture.width, startUV.y * drawableTexture.height);
        Vector2 endPixel = new Vector2(endUV.x * drawableTexture.width, endUV.y * drawableTexture.height);

        // 2. คำนวณระยะห่าง (เป็น pixel)
        float distance = Vector2.Distance(startPixel, endPixel);

        // 3. คำนวณ "ขนาดก้าว" ที่เหมาะสม
        // เราจะวาดจุดใหม่ทุกๆ 1/4 ของขนาดแปรง
        // เพื่อให้แน่ใจว่าวงกลมจะซ้อนทับกันสนิท
        float brushRadius = brushSize / 2.0f;
        float stepSize = Mathf.Max(1.0f, brushRadius * 0.25f); // ก้าวทีละ 1/4 ของรัศมี (หรืออย่างน้อย 1 pixel)

        // 4. คำนวณจำนวนก้าวที่ต้องใช้
        int steps = (int)Mathf.Ceil(distance / stepSize);
        steps = Mathf.Max(1, steps); // อย่างน้อย 1 ก้าว (คือจุดสิ้นสุด)

        // 5. วน Loop วาดจุดไปเรื่อยๆ
        for (int i = 0; i <= steps; i++)
        {
            // t คือ % ระยะทาง (0.0 ถึง 1.0)
            float t = (float)i / steps;

            // คำนวณ UV ที่อยู่ระหว่างทางโดยใช้ Lerp
            Vector2 interpolatedUV = Vector2.Lerp(startUV, endUV, t);

            // วาดวงกลม (แบบขอบฟุ้ง) ที่ตำแหน่งระหว่างทาง
            DrawOnTexture(interpolatedUV);
        }
    }


    // นี่คือหัวใจหลักของการวาด
    // (สำคัญ!) เปลี่ยนจาก private (ไม่มีอะไรนำหน้า) เป็น public
    // เพื่อให้ ShowDrawing เรียกใช้ได้
    public void DrawOnTexture(Vector2 uvCoordinate)
    {
        if (drawableTexture == null) return;

        int pixelX = (int)(uvCoordinate.x * drawableTexture.width);
        int pixelY = (int)(uvCoordinate.y * drawableTexture.height);

        // --- [SMOOTH BRUSH LOGIC START] ---

        // 1. คำนวณรัศมีเป็น float เพื่อความแม่นยำ
        float brushRadius = brushSize / 2.0f;

        // 2. กำหนดขอบเขตการวน loop ให้กว้างพอ (ปัดเศษขึ้น)
        // (ของเดิมใช้ int division ซึ่งอาจพลาดขอบไป)
        int intRadius = (int)Mathf.Ceil(brushRadius);

        for (int y = -intRadius; y <= intRadius; y++)
        {
            for (int x = -intRadius; x <= intRadius; x++)
            {
                // 3. คำนวณระยะห่าง (distance) ของ pixel นี้จากจุดศูนย์กลางแปรง
                float distance = new Vector2(x, y).magnitude;

                // 4. ถ้าอยู่นอกรัศมีแปรง (ที่เป็น float) ก็ข้ามไปเลย
                if (distance > brushRadius)
                {
                    continue;
                }

                // 5. คำนวณตำแหน่ง pixel ที่จะวาด
                int targetX = pixelX + x;
                int targetY = pixelY + y;

                // 6. เช็กว่า pixel อยู่ในขอบเขต Texture หรือไม่ (เหมือนเดิม)
                if (targetX >= 0 && targetX < drawableTexture.width &&
                    targetY >= 0 && targetY < drawableTexture.height)
                {
                    // --- [หัวใจของความ Smooth] ---

                    // 7. คำนวณ "ความเข้ม" (strength) ของการลงสี
                    // โดยใช้ระยะห่าง (distance) มาเทียบกับรัศมี (brushRadius)
                    // (0.0 = ขอบสุด, 1.0 = กลางสุด)
                    float falloff = distance / brushRadius;

                    // 8. ใช้ SmoothStep เพื่อให้ขอบมันฟุ้งๆ (feathered) สวยงาม
                    // (ถ้าอยากให้ขอบคมๆ แต่ยังเป็นวงกลม ให้ใช้: float strength = 1.0f - falloff;)
                    float strength = Mathf.SmoothStep(1.0f, 0.0f, falloff);

                    // 9. (สำคัญ) ต้องดึงสีเดิมของ pixel นั้นๆ ออกมาก่อน
                    // นี่คือส่วนที่ทำให้ช้าลงเล็กน้อย แต่จำเป็นสำหรับการผสมสี
                    Color originalColor = drawableTexture.GetPixel(targetX, targetY);

                    // 10. ผสมสี (Lerp) ระหว่าง "สีเดิม" กับ "สีใหม่"
                    // โดยใช้ "ความเข้ม (strength)" และ "alpha ของสีใหม่" เป็นตัวกำหนด
                    float blendAlpha = strength * paintColor.a;
                    Color blendedColor = Color.Lerp(originalColor, paintColor, blendAlpha);

                    // 11. สั่ง SetPixel ด้วยสีที่ผสมแล้ว
                    drawableTexture.SetPixel(targetX, targetY, blendedColor);

                    // --- [จบส่วนความ Smooth] ---
                }
            }
        }

        // --- [SMOOTH BRUSH LOGIC END] ---

        textureNeedsUpdate = true;
    }
}
