using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

[System.Serializable]
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

    [BoxGroup("1. InteractionSettings")]
    [Tooltip("ปุ่มที่ใช้กด (เช่น F, E)")]
    public KeyCode interactionKey = KeyCode.F;

    [BoxGroup("1. Interaction Settings")]
    [Tooltip("ติ๊กถูก ถ้าต้องการให้ Trigger นี้ 'เริ่มเกมมาแบบล็อค' (ต้องมีคนมาสั่งปลดล็อค)")]
    public bool startLocked = true;


    [BoxGroup("2. Delay & Cooldown")]
    [Tooltip("หน่วงเวลาก่อนที่ 'Success Sequence' *ทั้งหมด* จะเริ่มทำงาน")]
    [MinValue(0f)]
    public float preInteractionDelay = 0f;

    [BoxGroup("2. Delay & Cooldown")]
    [Tooltip("หน่วงเวลาก่อนที่ 'Fail Event' จะทำงาน")]
    [MinValue(0f)]
    public float preFailureDelay = 0f;

    [BoxGroup("2. Delay & Cooldown")]
    [Tooltip("เวลาหน่วง (วินาที) ก่อนที่จะกด 'สำเร็จ' ซ้ำได้")]
    [MinValue(0f)]
    public float interactionCooldown = 1.0f;


    [BoxGroup("3. Success Events (ทำงานเรียงลำดับ)")]
    [ReorderableList]
    public List<DelayedEvent> onInteractSuccessList;


    [BoxGroup("4. Fail Event (ทำงานครั้งเดียว)")]
    [Tooltip("Event ที่จะทำงานเมื่อกด 'ล้มเหลว' (ถ้ามันยังล็อคอยู่)")]
    public UnityEvent onInteractFail;


    [BoxGroup("5. Debug State")]
    [ReadOnly] private bool isPlayerInside = false;
    [ReadOnly][SerializeField] private bool isUnlocked = false;
    [ReadOnly] private bool isBusy = false; // << (ปรับปรุง) ใช้ตัวแปรนี้ตัวเดียวคุมทั้ง Success และ Fail

    // (ใหม่!) ตัวแปรสำหรับ Caching Yield Instructions
    private WaitForSeconds _preInteractionYield;
    private WaitForSeconds _preFailureYield;
    private WaitForSeconds _cooldownYield;
    // (เราจะไม่ cache delay ใน List เพราะมัน dynamic และถูกสร้างไม่บ่อยเท่า)


    #region "Core Logic (Optimized Awake)"
    private void Awake()
    {
        isUnlocked = !startLocked;

        // (ใหม่!) ทำการ Cache WaitForSeconds เพื่อลด GC Allocations
        // เราเช็ก > 0f เพื่อไม่ให้สร้าง object ถ้าไม่จำเป็น
        if (preInteractionDelay > 0f)
            _preInteractionYield = new WaitForSeconds(preInteractionDelay);

        if (preFailureDelay > 0f)
            _preFailureYield = new WaitForSeconds(preFailureDelay);

        if (interactionCooldown > 0f)
            _cooldownYield = new WaitForSeconds(interactionCooldown);
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
        // ยังคงใช้ isPlayerInside เช็กก่อน เพื่อให้ Coroutine ทำงานต่อได้
        // แม้ผู้เล่นจะเดินออกจาก Trigger ไปแล้ว
        if (isPlayerInside && Input.GetKeyDown(interactionKey))
        {
            AttemptInteraction();
        }
    }
    #endregion

    /// <summary>
    /// (ปรับปรุง!) แก้ไขตรรกะการเช็กให้รัดกุมขึ้น
    /// </summary>
    private void AttemptInteraction()
    {
        // เช็ก isBusy ก่อนเป็นอันดับแรก
        // ถ้ากำลังยุ่ง (ไม่ว่าจะ Success หรือ Fail) ให้ return ทันที
        if (isBusy)
        {
            return;
        }

        // ถ้าไม่ยุ่ง ให้ล็อคทันที
        isBusy = true;

        if (isUnlocked)
        {
            // ถ้าปลดล็อค -> เริ่ม Success
            StartCoroutine(SuccessRoutine());
        }
        else
        {
            // ถ้ายังล็อค -> เริ่ม Fail
            StartCoroutine(FailRoutine());
        }
    }

    public void CallReturnToMenu()
    {
        GameManager.Instance.ChangeState(GameState.MainMenu);
        if (GameSceneManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
            Cursor.lockState = CursorLockMode.None;   // <--- นี่ไงครับ
            Cursor.visible = true;

            Debug.Log("Wrapper: กำลังเรียก ReturnToMenu() จาก Instance...");
            GameSceneManager.Instance.ReturnToMenu();

        }
        else
        {
            Debug.LogError("หา GameSceneManager.Instance ไม่เจอ!");
        }
    }

    public void CallLoadNextLevel(string name)
    {
        GameManager.Instance.ChangeState(GameState.Playing);
        if (GameSceneManager.Instance != null)
        {
            Debug.Log("Wrapper: กำลังเรียก LoadNextLevel() จาก Instance...");
            GameSceneManager.Instance.LoadSceneAsync(name);
        }
        else
        {
            Debug.LogError("หา GameSceneManager.Instance ไม่เจอ!");
        }
    }



    /// <summary>
    /// (ปรับปรุง!) ยุบ CooldownRoutine มารวมที่นี่
    /// และใช้ _preInteractionYield, _cooldownYield ที่ Cache ไว้
    /// </summary>
    private IEnumerator SuccessRoutine()
    {
        Debug.Log("Interaction กำลังเริ่ม (รอ pre-delay)...");

        // 1. รอ "ดีเลย์เริ่มต้น" (ถ้ามี)
        if (preInteractionDelay > 0f)
            yield return _preInteractionYield; // (ใช้ Cache)

        Debug.Log("เริ่มทำงาน Success Sequence...");

        // 2. วนลูป Event ทั้งหมดใน List
        foreach (DelayedEvent delayedEvent in onInteractSuccessList)
        {
            // 2a. รอ "ดีเลย์คั่น"
            if (delayedEvent.delay > 0f)
                yield return new WaitForSeconds(delayedEvent.delay); // (ยัง new อยู่ เพราะเป็นค่า dynamic)

            // 2b. สั่งทำงาน Event
            if (delayedEvent.action != null)
            {
                Debug.Log("Invoking delayed event...");
                delayedEvent.action.Invoke();
            }
        }

        Debug.Log("Success Sequence จบแล้ว, เริ่ม Cooldown...");

        // 3. รอ Cooldown (ถ้ามี)
        if (interactionCooldown > 0f)
            yield return _cooldownYield; // (ใช้ Cache)

        // 4. ปลดล็อค isBusy
        isBusy = false;
    }

    /// <summary>
    /// (ปรับปรุง!) ใช้ _preFailureYield ที่ Cache ไว้
    /// และเปลี่ยนมาปลดล็อค isBusy แทนตัวแปร isCheckingFail
    /// </summary>
    private IEnumerator FailRoutine()
    {
        // isBusy ถูกตั้งค่าเป็น true ใน AttemptInteraction() แล้ว

        if (preFailureDelay > 0f)
            yield return _preFailureYield; // (ใช้ Cache)

        Debug.Log("Interaction ล้มเหลว! ประตูยังล็อคอยู่");
        onInteractFail.Invoke();

        // ปลดล็อค isBusy เพื่อให้กดครั้งต่อไปได้
        // (Fail ไม่มี Cooldown, จึงปลดล็อคทันที)
        isBusy = false;
    }


    // (ลบออก) CooldownRoutine() ถูกยุบรวมเข้ากับ SuccessRoutine() แล้ว
    // private IEnumerator CooldownRoutine()...


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