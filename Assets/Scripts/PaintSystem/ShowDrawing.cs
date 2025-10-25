using UnityEngine;
using System.Collections; 

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshCollider))] // (เพิ่ม!) ต้องมี Collider เพื่อให้วาดทับได้
public class ShowDrawing : MonoBehaviour
{
    public SignPainter sourceSign; 
    
    private Renderer myRenderer;
    private bool isTextureSet = false;
    private Camera playerCamera; // (เพิ่ม) ต้องมีกล้อง

    void Start()
    {
        myRenderer = GetComponent<Renderer>();
        playerCamera = Camera.main; // (เพิ่ม)

        if (sourceSign == null)
        {
            Debug.LogError("ShowDrawing: ยังไม่ได้ลากป้ายต้นฉบับ (Source Sign) มาใส่!");
            return;
        }

        StartCoroutine(TryToGetTextureCoroutine());
    }
    
    // (เพิ่ม Update)
    void Update()
    {
        // ถ้ายังเชื่อมต่อ Texture ไม่สำเร็จ (isTextureSet == false)
        // หรือ ถ้าไม่มี sourceSign (เผื่อไว้) ก็ไม่ต้องทำอะไร
        if (!isTextureSet || sourceSign == null)
        {
            return;
        }

        // --- ตรรกะการวาด (สำหรับ Object 2) ---
        if (Input.GetMouseButton(0))
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // เช็กว่าชน "ตัวเอง" (Object 2) หรือไม่
                if (hit.collider == GetComponent<Collider>())
                {
                    // --- หัวใจสำคัญ ---
                    // ถ้าใช่, ให้ "ส่งคำสั่งวาด" กลับไปที่ Object 1
                    // โดยใช้พิกัด UV ที่ได้จาก Object 2
                    sourceSign.DrawOnTexture(hit.textureCoord);
                }
            }
        }
    }

    // Coroutine ส่วนนี้เหมือนเดิม
    IEnumerator TryToGetTextureCoroutine()
    {
        Debug.Log("ShowDrawing: กำลังรอ Texture จาก Source Sign...");
        
        while (!isTextureSet)
        {
            Texture2D textureFromSign = sourceSign.GetDrawableTexture();
            if (textureFromSign != null)
            {
                Debug.Log("ShowDrawing: ได้รับ Texture แล้ว! กำลังเชื่อมต่อ...");
                myRenderer.material.mainTexture = textureFromSign;
                isTextureSet = true; 
            }
            else
            {
                Debug.LogWarning("ShowDrawing: Texture ยังไม่พร้อม... จะลองใหม่ใน 0.2 วินาที");
                yield return new WaitForSeconds(0.2f); 
            }
        }
        
        Debug.Log("ShowDrawing: เชื่อมต่อ Real-time สำเร็จ!");
    }
}