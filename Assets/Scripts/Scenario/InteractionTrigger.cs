using UnityEngine;
using UnityEngine.Events;
using System.Collections; // << ต้องมี เพื่อใช้ Coroutine

public class InteractionTrigger : MonoBehaviour
{
    [Header("การตั้งค่า Interaction")]
    [Tooltip("แท็กของ Object ที่จะให้ Trigger ทำงาน (ปกติคือ 'Player')")]
    public string triggerTag = "Player";
    [Tooltip("ปุ่มที่ใช้กด (เช่น F, E)")]
    public KeyCode interactionKey = KeyCode.F;

    // --- (ใหม่!) ---
    [Tooltip("เวลาหน่วง (วินาที) ก่อนที่จะกดซ้ำได้ (แก้การกดรัว)")]
    public float interactionCooldown = 1.0f;
    // --- (จบส่วนใหม่) ---

    [Header("สถานะการล็อค")]
    [Tooltip("ติ๊กถูก ถ้าต้องการให้ Trigger นี้ 'เริ่มเกมมาแบบล็อค' (ต้องมีคนมาสั่งปลดล็อค)")]
    public bool startLocked = true;

    [Header("Events")]
    [Tooltip("Event ที่จะทำงานเมื่อกด 'สำเร็จ' (ถ้ามันปลดล็อคอยู่)")]
    public UnityEvent onInteractSuccess;
    [Tooltip("Event ที่จะทำงานเมื่อกด 'ล้มเหลว' (ถ้ามันยังล็อคอยู่)")]
    public UnityEvent onInteractFail;

    // --- ตัวแปรภายใน ---
    private bool isPlayerInside = false;
    [SerializeField] private bool isUnlocked = false; // ตัวแปรสถานะการล็อค
    private bool isBusy = false; // (ใหม่!) ตัวแปรกันการกดรัว

    private void Awake()
    {
        // ตั้งค่าสถานะเริ่มต้น
        isUnlocked = !startLocked;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerTag))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(triggerTag))
        {
            isPlayerInside = false;
        }
    }

    private void Update()
    {
        if (isPlayerInside && Input.GetKeyDown(interactionKey))
        {
            AttemptInteraction(); // ลองทำงาน
        }
    }

    private void AttemptInteraction()
    {
        // (แก้ไข!) เช็คว่า 'isUnlocked' และ 'ไม่ Busy'
        if (isUnlocked && !isBusy)
        {
            // ---- สำเร็จ ----

            // (สำคัญ!) 1. ตั้งค่า 'Busy' *ก่อน* สั่ง Invoke()
            // เผื่อว่า Event นี้สั่งย้าย Scene หรือทำลาย Object นี้
            isBusy = true;

            Debug.Log("Interaction สำเร็จ!");

            // 2. สั่งทำงาน Event
            onInteractSuccess.Invoke();

            // 3. สั่งเริ่ม Cooldown
            // (ถ้า Invoke ข้างบนสั่งย้าย Scene บรรทัดนี้จะไม่ทำงาน...
            // ...ซึ่งก็ไม่เป็นไร เพราะ Object นี้กำลังจะถูกทำลายอยู่แล้ว)
            StartCoroutine(CooldownRoutine());
        }
        else if (!isUnlocked)
        {
            // ---- ล้มเหลว (ล็อค) ----
            Debug.Log("Interaction ล้มเหลว! ประตูยังล็อคอยู่");
            onInteractFail.Invoke();
        }
        // (ถ้า 'isBusy' อยู่, การกด F จะไม่ทำอะไรเลย)
    }

    /// <summary>
    /// (ใหม่!) Coroutine สำหรับนับ Cooldown
    /// </summary>
    private IEnumerator CooldownRoutine()
    {
        // รอตามเวลาที่ตั้งค่าไว้ใน Inspector
        yield return new WaitForSeconds(interactionCooldown);

        // เมื่อครบเวลา, รีเซ็ตสถานะ 'Busy'
        isBusy = false;
    }


    /// <summary>
    /// (PUBLIC) Function ที่สำคัญที่สุด!
    /// เอาไว้ให้ Script อื่น (เช่น ถังน้ำมัน) เรียกใช้เพื่อ 'ปลดล็อค' Trigger นี้
    /// </summary>
    public void Unlock()
    {
        isUnlocked = true;
        Debug.Log(name + " ถูกปลดล็อคแล้ว!");
    }
}