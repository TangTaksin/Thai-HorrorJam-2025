using TMPro;
using UnityEngine;
using System.Collections; // <-- ต้องมีบรรทัดนี้

public class PointerInfoDisplay : MonoBehaviour
{
    // --- 1. สร้าง Singleton Instance ---
    public static PointerInfoDisplay Instance { get; private set; }

    TextMeshProUGUI pointerTxt;

    // --- 2. ตัวแปรใหม่สำหรับ Feedback ---
    private Coroutine _feedbackCoroutine;
    private bool _isShowingFeedback = false;

    private void Awake()
    {
        // ตั้งค่า Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        pointerTxt = GetComponent<TextMeshProUGUI>();
        pointerTxt.text = string.Empty;
    }

    private void OnEnable()
    {
        FPInteract.OnDetectedInteractableChanged += UpdatePointerText;
    }

    private void OnDisable()
    {
        FPInteract.OnDetectedInteractableChanged -= UpdatePointerText;
    }

    void UpdatePointerText(IInteractable interactable)
    {
        // 3. ถ้ากำลังแสดง Feedback (เช่น "Locked", "Cooldown")
        //    ให้ข้ามการอัปเดต UI ปกติ ( [ F ] ... )
        if (_isShowingFeedback) return;

        var itemObj = interactable as ItemObject;
        var isntNull = itemObj != null;

        if (isntNull)
            pointerTxt.text = $"[ F ] {itemObj.itemDisplayName}";
        else
            pointerTxt.text = string.Empty;
    }

    // --- 4. ฟังก์ชัน Feedback สาธารณะใหม่ ---
    /// <summary>
    /// แสดงข้อความ Feedback ชั่วคราว (เช่น "Locked", "On Cooldown")
    /// </summary>
    /// <param name="message">ข้อความที่จะแสดง</param>
    /// <param name="duration">ระยะเวลา (วินาที)</param>
    public void ShowTemporaryFeedback(string message, float duration)
    {
        // ถ้ามี Feedback เก่าค้างอยู่ ให้หยุดมันก่อน
        if (_feedbackCoroutine != null)
        {
            StopCoroutine(_feedbackCoroutine);
        }
        
        // เริ่ม Coroutine ใหม่
        _feedbackCoroutine = StartCoroutine(FeedbackCoroutine(message, duration));
    }


    // --- 5. Coroutine ใหม่สำหรับจัดการ Feedback ---
    private IEnumerator FeedbackCoroutine(string message, float duration)
    {
        _isShowingFeedback = true;
        pointerTxt.text = message; // <--- แสดงข้อความ Feedback ที่ได้รับมา

        yield return new WaitForSeconds(duration);

        _isShowingFeedback = false;
        _feedbackCoroutine = null;

        // 6. คืนค่า UI ให้กลับเป็นปกติ
        // (จำเป็นต้องใช้ FPInteract.Instance)
        if (FPInteract.Instance != null)
        {
            UpdatePointerText(FPInteract.Instance.DetectedInteractable);
        }
        else
        {
            pointerTxt.text = string.Empty;
        }
    }
}