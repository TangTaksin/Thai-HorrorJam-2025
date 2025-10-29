using UnityEngine;

// เรายังคง [RequireComponent] ไว้ เผื่อว่าคุณอยากให้มันชนอะไร
[RequireComponent(typeof(Rigidbody))]
public class CarMovement : MonoBehaviour
{
    // 1. เปลี่ยนชื่อจาก Force (แรง) เป็น Speed (ความเร็ว)
    //    เพราะ transform.Translate ใช้วัดเป็น "ความเร็ว"
    //    ค่า 2000 จะเร็วไปมาก, 10-20 กำลังดี
    [Tooltip("ความเร็วที่รถจะเคลื่อนที่ไปข้างหน้า")]
    public float moveSpeed = 15.0f;

    // 2. สร้าง "สวิตช์" ควบคุมการเคลื่อนที่
    private bool isMoving = false;
    
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // 3. (สำคัญมาก!) ตั้งค่า Rigidbody เป็น Kinematic
        //    เพราะเราควบคุมด้วย transform.Translate
        //    ถ้าไม่ตั้งเป็น Kinematic สคริปต์จะ "ตีกัน" กับระบบฟิสิกส์
        rb.isKinematic = true; 
    }

    void Update()
    {
        // 4. เช็ค "สวิตช์" ก่อน
        //    ถ้า isMoving เป็น true เท่านั้น ถึงจะขยับ
        if (isMoving)
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }

    // 5. --- นี่คือ "Event" หรือ "ตัวเรียก" ที่คุณต้องการ ---
    //    ฟังก์ชัน Public ที่สคริปต์อื่น หรือปุ่ม หรือ Timeline สามารถเรียกได้
    public void StartMoving()
    {
        isMoving = true; // สั่งเปิด "สวิตช์"
    }

    // (แถม) สร้างฟังก์ชันสั่งหยุดไว้ด้วยก็ได้
    public void StopMoving()
    {
        isMoving = false; // สั่งปิด "สวิตช์"
    }
}