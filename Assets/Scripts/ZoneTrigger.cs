using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ZoneTrigger : MonoBehaviour
{
    [Header("การตั้งค่า Trigger")]
    [Tooltip("แท็กของ Object ที่จะให้ Trigger ทำงาน (ปกติคือ 'Player')")]
    public string triggerTag = "Player";

    [Header("Event ตอนเข้า (On Enter)")]
    [Tooltip("ติ๊กถูก ถ้าต้องการให้ Event 'ตอนเข้า' ทำงานแค่ครั้งเดียว")]
    public bool triggerOnce = true;
    
    [Tooltip("หน่วงเวลาก่อนที่ 'Event ตอนเข้า' จะทำงาน (วินาที)")]
    public float enterDelay = 0f;

    [Tooltip("Event ที่จะให้ทำงานเมื่อ Player 'เดินเข้า' Trigger")]
    public UnityEvent onPlayerEnter;

    [Header("Event ตอนออก (On Exit)")]
    [Tooltip("หน่วงเวลาก่อนที่ 'Event ตอนออก' จะทำงาน (วินาที)")]
    public float exitDelay = 0f;

    [Tooltip("Event ที่จะให้ทำงานเมื่อ Player 'เดินออก' จาก Trigger")]
    public UnityEvent onPlayerExit;

    // --- (แก้ไข!) ---
    [Header("การตั้งค่าหลังทำงาน")]
    [Tooltip("ติ๊กถูก ถ้าต้องการ 'ทำลาย' GameObject นี้ หลังจาก Event 'ตอนออก' ทำงานเสร็จ")]
    public bool destroyAfterExit = false; // เปลี่ยนชื่อจาก destroyAfterEnter
    // --- (จบส่วนแก้ไข) ---

    // --- ตัวแปรภายใน ---
    private bool hasBeenTriggered = false;
    private bool isPlayerInside = false;

    /// <summary>
    /// Function นี้จะทำงานอัตโนมัติเมื่อมี Collider อื่นเข้ามาในเขต Trigger
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerTag) && !isPlayerInside)
        {
            isPlayerInside = true;

            if (triggerOnce && hasBeenTriggered)
            {
                return;
            }

            Debug.Log(other.name + " ได้เข้ามาใน " + this.name);
            
            if (enterDelay > 0)
            {
                StartCoroutine(ExecuteEnterEventAfterDelay(enterDelay));
            }
            else
            {
                // ทำงานทันที (ไม่มี Delay)
                onPlayerEnter.Invoke();
                
                // (ลบการตรวจสอบ Destroy ออกจากตรงนี้)
            }
            
            hasBeenTriggered = true;
        }
    }

    /// <summary>
    /// Function นี้จะทำงานอัตโนมัติเมื่อ Collider ที่เคยอยู่ข้างใน "ออก" จากเขต Trigger
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(triggerTag) && isPlayerInside)
        {
            isPlayerInside = false;
            Debug.Log(other.name + " ได้ออกจาก " + this.name);
            StartCoroutine(ExecuteExitEventAfterDelay(exitDelay));
        }
    }

    /// <summary>
    /// Coroutine สำหรับ Event "ตอนเข้า"
    /// </summary>
    private IEnumerator ExecuteEnterEventAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        onPlayerEnter.Invoke();

        // (ลบการตรวจสอบ Destroy ออกจากตรงนี้)
    }

    /// <summary>
    /// Coroutine สำหรับ Event "ตอนออก" (แก้ Bug การสั่น)
    /// </summary>
    private IEnumerator ExecuteExitEventAfterDelay(float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        else
        {
            yield return null; // รอ 1 เฟรม
        }

        if (!isPlayerInside) // เช็คอีกครั้งว่า Player ออกไปจริงๆ
        {
            onPlayerExit.Invoke();

            // (ใหม่!) ตรวจสอบว่าต้องทำลายตัวเองหรือไม่ (หลังจาก Event 'ตอนออก' ทำงาน)
            if (destroyAfterExit)
            {
                Destroy(gameObject); // ทำลาย GameObject นี้
            }
        }
        else
        {
            Debug.Log("OnTriggerExit ถูกยกเลิก เพราะตรวจพบการสั่น (Flicker)");
        }
    }


    /// <summary>
    /// (PUBLIC) Function สำหรับรีเซ็ต Trigger
    /// </summary>
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
        Debug.Log(name + " Trigger ได้ถูกรีเซ็ตแล้ว!");
    }


    private void OnDrawGizmos()
    {
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.color = new Color(0, 1, 0, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCol.center, boxCol.size);
            Gizmos.matrix = oldMatrix;
        }
        else
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.4f);
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}