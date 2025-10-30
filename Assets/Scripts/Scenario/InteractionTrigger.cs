using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic; // << (ใหม่!) ต้องมีเพื่อใช้ List<>
using NaughtyAttributes; 

// (ใหม่!) สร้าง Class สำหรับเก็บ Event ที่มีดีเลย์
[System.Serializable] // << ทำให้มันโชว์ใน Inspector
public class DelayedEvent
{
    [Tooltip("หน่วงเวลากี่วินาที *ก่อน* ที่ Event นี้จะเริ่มทำงาน")]
    [MinValue(0f)]
    public float delay = 0f;
    
    [Tooltip("Event ที่จะให้ทำงาน")]
    public UnityEvent action;
}


public class InteractionTrigger : MonoBehaviour
{
    [BoxGroup("1. Interaction Settings")]
    [Tooltip("แท็กของ Object ที่จะให้ Trigger ทำงาน (ปกติคือ 'Player')")]
    public string triggerTag = "Player";
    
    [BoxGroup("1. Interaction Settings")]
    [Tooltip("ปุ่มที่ใช้กด (เช่น F, E)")]
    public KeyCode interactionKey = KeyCode.F;

    [BoxGroup("1. Interaction Settings")]
    [Tooltip("ติ๊กถูก ถ้าต้องการให้ Trigger นี้ 'เริ่มเกมมาแบบล็อค' (ต้องมีคนมาสั่งปลดล็อค)")]
    public bool startLocked = true;

    
    [BoxGroup("2. Delay & Cooldown")]
    [Tooltip("หน่วงเวลาก่อนที่ 'Success Sequence' *ทั้งหมด* จะเริ่มทำงาน")]
    [MinValue(0f)]
    public float preInteractionDelay = 0f; // << ดีเลย์ "ก่อน" เริ่มทั้งขบวน

    [BoxGroup("2. Delay & Cooldown")]
    [Tooltip("หน่วงเวลาก่อนที่ 'Fail Event' จะทำงาน")]
    [MinValue(0f)]
    public float preFailureDelay = 0f;

    [BoxGroup("2. Delay & Cooldown")]
    [Tooltip("เวลาหน่วง (วินาที) ก่อนที่จะกด 'สำเร็จ' ซ้ำได้")]
    [MinValue(0f)]
    public float interactionCooldown = 1.0f;
    

    // --- (อัปเกรด!) ---
    [BoxGroup("3. Success Events (ทำงานเรียงลำดับ)")]
    [ReorderableList] // << ทำให้ List นี้จัดลำดับได้ใน Inspector
    public List<DelayedEvent> onInteractSuccessList; // << เปลี่ยนจาก UnityEvent เดียวเป็น List
    // --- (จบส่วนอัปเกรด) ---

    
    [BoxGroup("4. Fail Event (ทำงานครั้งเดียว)")]
    [Tooltip("Event ที่จะทำงานเมื่อกด 'ล้มเหลว' (ถ้ามันยังล็อคอยู่)")]
    public UnityEvent onInteractFail;

    
    [BoxGroup("5. Debug State")]
    [ReadOnly] private bool isPlayerInside = false;
    [ReadOnly] [SerializeField] private bool isUnlocked = false; 
    [ReadOnly] private bool isBusy = false; 
    [ReadOnly] private bool isCheckingFail = false; 

    // (ส่วน Awake, OnTriggerEnter, OnTriggerExit, Update ... ยังเหมือนเดิมทุกอย่าง)
    #region "Core Logic (No Change)"
    private void Awake()
    {
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
            AttemptInteraction(); 
        }
    }
    #endregion

    private void AttemptInteraction()
    {
        if (isUnlocked && !isBusy)
        {
            isBusy = true; 
            StartCoroutine(SuccessRoutine()); // << (เรียก Coroutine ที่อัปเกรดแล้ว)
        }
        else if (!isUnlocked && !isCheckingFail)
        {
            StartCoroutine(FailRoutine());
        }
    }

    /// <summary>
    /// (อัปเกรด!) Coroutine นี้จะวนลูปทำงาน Event ทีละตัวตาม List
    /// </summary>
    private IEnumerator SuccessRoutine()
    {
        Debug.Log("Interaction กำลังเริ่ม (รอ pre-delay)...");

        // 1. รอ "ดีเลย์เริ่มต้น" (ถ้ามี)
        if (preInteractionDelay > 0f)
            yield return new WaitForSeconds(preInteractionDelay);

        Debug.Log("เริ่มทำงาน Success Sequence...");

        // 2. วนลูป Event ทั้งหมดใน List
        foreach (DelayedEvent delayedEvent in onInteractSuccessList)
        {
            // 2a. รอ "ดีเลย์คั่น" ที่กำหนดไว้ในแต่ละ Event
            if (delayedEvent.delay > 0f)
                yield return new WaitForSeconds(delayedEvent.delay);

            // 2b. สั่งทำงาน Event
            if (delayedEvent.action != null)
            {
                Debug.Log("Invoking delayed event...");
                delayedEvent.action.Invoke();
            }
        }

        Debug.Log("Success Sequence จบแล้ว, เริ่ม Cooldown...");

        // 3. สั่งเริ่ม Cooldown 'หลัง' ทำงาน (เมื่อทุกอย่างใน List ทำเสร็จแล้ว)
        StartCoroutine(CooldownRoutine());
    }

    /// <summary>
    /// (ยังเหมือนเดิม) Coroutine สำหรับการกดล้มเหลว
    /// </summary>
    private IEnumerator FailRoutine()
    {
        isCheckingFail = true; 

        if (preFailureDelay > 0f)
            yield return new WaitForSeconds(preFailureDelay);

        Debug.Log("Interaction ล้มเหลว! ประตูยังล็อคอยู่");
        onInteractFail.Invoke();

        isCheckingFail = false; 
    }


    /// <summary>
    /// (ยังเหมือนเดิม) Coroutine สำหรับนับ Cooldown
    /// </summary>
    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(interactionCooldown);
        isBusy = false;
    }


    /// <summary>
    /// (ยังเหมือนเดิม) Public Functions สำหรับ ปลดล็อค/ล็อค
    /// </summary>
    [Button("Test Unlock", EButtonEnableMode.Playmode)] 
    public void Unlock()
    {
        isUnlocked = true;
        Debug.Log(name + " ถูกปลดล็อคแล้ว!");
    }

    [Button("Test Lock", EButtonEnableMode.Playmode)] 
    public void Lock()
    {
        isUnlocked = false;
        Debug.Log(name + " ถูกล็อค!");
    }
}