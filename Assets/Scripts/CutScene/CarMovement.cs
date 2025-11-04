using UnityEngine;
using NaughtyAttributes; // อย่าลืม import NaughtyAttributes

// เรายังคง [RequireComponent] ไว้ เผื่อว่าคุณอยากให้มันชนอะไร
[RequireComponent(typeof(Rigidbody))]
public class CarMovement : MonoBehaviour
{
    // 1. สร้าง enum เพื่อกำหนดทิศทาง
    //    เราสามารถเรียกใช้ enum นี้จากสคริปต์อื่นได้ด้วย
    public enum MovementDirection
    {
        None,
        Forward,
        Backward,
        Left,
        Right
    }

    [BoxGroup("Movement Settings")]
    [Tooltip("ความเร็วที่รถจะเคลื่อนที่ (หน่วย: เมตรต่อวินาที)")]
    [MinValue(0f)]
    public float moveSpeed = 15.0f;

    // 2. เปลี่ยน "สวิตช์" (bool) มาเป็น "สถานะ" (enum)
    [BoxGroup("Current State")]
    [Tooltip("ทิศทางที่กำลังเคลื่อนที่อยู่ในปัจจุบัน")]
    [SerializeField, ReadOnly] // ใช้ [SerializeField] เพื่อให้ private field โชว์ใน Inspector
    private MovementDirection currentDirection = MovementDirection.None;
    
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // 3. (สำคัญมาก!) ตั้งค่า Rigidbody เป็น Kinematic
        //    เพราะเราควบคุมด้วย transform.Translate
        rb.isKinematic = true; 
    }

    void Update()
    {
        // 4. เช็ค "สถานะ" ปัจจุบันด้วย switch case
        //    แทนการใช้ if (isMoving) แบบเดิม
        switch (currentDirection)
        {
            case MovementDirection.Forward:
                // transform.Translate จะเคลื่อนที่ตามแกน "local" ของ Object
                // Vector3.forward คือ "ไปข้างหน้า" ของรถ
                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
                break;
                
            case MovementDirection.Backward:
                transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);
                break;
                
            case MovementDirection.Left:
                transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
                break;
                
            case MovementDirection.Right:
                transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
                break;

            case MovementDirection.None:
                // ไม่ต้องทำอะไร (หยุดนิ่ง)
                break;
        }
    }

    // 5. --- นี่คือ "Event" หรือ "ตัวเรียก" ที่คุณต้องการ ---
    //    สร้างฟังก์ชัน Public แยกสำหรับแต่ละทิศทาง
    //    เพื่อให้ปุ่ม, Timeline หรือสคริปต์อื่นเรียกใช้ง่าย
    
    [Button("Move Forward", EButtonEnableMode.Playmode)] // ปุ่มทดสอบใน Inspector
    public void MoveForward()
    {
        currentDirection = MovementDirection.Forward;
    }

    [Button("Move Backward", EButtonEnableMode.Playmode)]
    public void MoveBackward()
    {
        currentDirection = MovementDirection.Backward;
    }

    [Button("Move Left", EButtonEnableMode.Playmode)]
    public void MoveLeft()
    {
        currentDirection = MovementDirection.Left;
    }

    [Button("Move Right", EButtonEnableMode.Playmode)]
    public void MoveRight()
    {
        currentDirection = MovementDirection.Right;
    }

    [Button("Stop Moving", EButtonEnableMode.Playmode)]
    public void StopMoving()
    {
        currentDirection = MovementDirection.None;
    }

    // (แถม) ฟังก์ชันเผื่อคุณอยาก Set ทิศทางจากสคริปต์อื่นโดยตรง
    public void SetDirection(MovementDirection newDirection)
    {
        currentDirection = newDirection;
    }
}