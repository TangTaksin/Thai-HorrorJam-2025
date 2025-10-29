using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes; // << ต้องมี NaughtyAttributes

/// <summary>
/// (Enum) ตัวเลือกทิศทาง Jumpscare
/// </summary>
public enum JumpScareDirection
{
    FlyTowardsFace,  // พุ่งเข้าหน้า (ค่าเริ่มต้น)
    FlyFromLeft,     // วิ่งจากซ้ายไปขวา
    FlyFromRight,    // วิ่งจากขวาไปซ้าย
    FlyFromTop,      // วิ่งจากบนลงล่าง
    FlyFromBottom,   // วิ่งจากล่างขึ้นบน
    FlyPastFace,     // วิ่งจากหลังไปหน้า
    Custom           // กำหนดเอง
}

/// <summary>
/// Script จัดการ Jumpscare ที่ทำงานผ่าน Trigger
/// </summary>
public class JumpScareManager : MonoBehaviour
{
    [Header("Jump Scare Settings")]
    [Tooltip("GameObject ที่จะโผล่ออกมา (ผี, สัตว์ประหลาด)")]
    [SerializeField] private GameObject scareObject;
    [Tooltip("เสียง Jumpscare (เสียงแหลม, เสียงคำราม)")]
    [SerializeField] private AudioClip scareSound;
    [Tooltip("ระยะเวลาโดยรวมของ Event (วินาที) (เช่น เสียง + สั่น)")]
    [SerializeField] private float scareDuration = 2f;
    [Tooltip("ติ๊กถูก ถ้าต้องการล็อคการควบคุมผู้เล่นชั่วคราว")]
    [SerializeField] private bool disablePlayerControl = true;

    [Header("Fly-by Path")]
    [Tooltip("เลือกทิศทาง Jumpscare ที่ตั้งค่าไว้ล่วงหน้า")]
    [SerializeField] private JumpScareDirection direction = JumpScareDirection.FlyTowardsFace;

    [Tooltip("จุดเริ่มต้น (ซ้าย/ขวา, บน/ล่าง, หน้า/หลัง) เทียบกับหน้ากล้อง")]
    [ShowIf("direction", JumpScareDirection.Custom)] // << ซ่อน ถ้าไม่ใช่ Custom
    [AllowNesting] 
    [SerializeField] private Vector3 startOffset = new Vector3(0f, 0f, 10f); // ค่าเริ่มต้น (พุ่งเข้าหน้า)

    [Tooltip("จุดสิ้นสุด (ซ้าย/ขวา, บน/ล่าง, หน้า/หลัง) เทียบกับหน้ากล้อง")]
    [ShowIf("direction", JumpScareDirection.Custom)] // << ซ่อน ถ้าไม่ใช่ Custom
    [AllowNesting] 
    [SerializeField] private Vector3 endOffset = new Vector3(0f, 0f, 0.5f); // ค่าเริ่มต้น (พุ่งเข้าหน้า)
    
    [Tooltip("ความเร็วที่ Object วิ่งผ่าน (วินาที)")]
    [SerializeField] private float flyByDuration = 0.5f;
    [Tooltip("รูปแบบการเคลื่อนไหว (เช่น Linear, EaseIn)")]
    [SerializeField] private AnimationCurve flyByCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Camera Shake")]
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float shakeDuration = 0.5f;

    [Header("Visual Effects")]
    [SerializeField] private bool enableScreenFlash = true;
    [SerializeField] private float screenFlashIntensity = 0.8f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float volumeScale = 1f;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerTag = "Player";
    
    [Header("Player Control (Specific Scripts)")]
    [Tooltip("รายชื่อ 'ชื่อ Class' ของ Script ที่จะปิดการทำงาน (เช่น PlayerMovement, MouseLook)")]
    [SerializeField] private List<string> scriptsToDisable = new List<string> 
    { 
        "FirstPersonController", 
        "PlayerMovement",
        "MouseLook"
    };

    // --- ตัวแปรภายใน ---
    private Camera mainCamera;
    private bool hasTriggered = false;
    private Vector3 originalCameraPos;
    private Dictionary<MonoBehaviour, bool> disabledScripts = new Dictionary<MonoBehaviour, bool>();
    private Texture2D flashTexture;
    private bool isFlashing = false;
    private float currentFlashAlpha = 0f;

    // --- ปุ่ม Test (NaughtyAttributes) ---
    [Button("Test Jump Scare")]
    public void TriggerJumpScare()
    {
        if (hasTriggered && triggerOnce)
        {
            Debug.LogWarning("JumpScare Test: ไม่สามารถรันได้ (TriggerOnce ทำงานไปแล้ว)");
            return;
        }
        
        hasTriggered = true;

        if (Application.isPlaying)
        {
            StartCoroutine(JumpScareSequence());
        }
        else
        {
            Debug.Log("JumpScare Test: กดปุ่มทำงาน (ต้องอยู่ใน Play Mode เพื่อดูผลลัพธ์เต็มรูปแบบ)");
        }
    }

    [Button("Reset Trigger")]
    public void ResetTrigger()
    {
        hasTriggered = false;
        Debug.Log("Jump Scare Trigger has been reset.");
    }
    // --- จบส่วนปุ่ม Test ---

    private void Start()
    {
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("JumpScareManager: Main Camera not found!");
            enabled = false;
            return;
        }

        // if (scareObject != null)
        //     scareObject.SetActive(false);

        // ตรวจสอบ AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }

        CreateFlashTexture();
    }

    private void CreateFlashTexture()
    {
        flashTexture = new Texture2D(1, 1);
        flashTexture.SetPixel(0, 0, Color.white);
        flashTexture.Apply();
    }

    /// <summary>
    /// ทำงานเมื่อ Player แตะ Collider
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;

        TriggerJumpScare();
    }

    /// <summary>
    /// Coroutine หลัก ควบคุมลำดับเหตุการณ์ทั้งหมด
    /// </summary>
    private IEnumerator JumpScareSequence()
    {
        originalCameraPos = mainCamera.transform.localPosition;

        if (disablePlayerControl)
            DisablePlayerControls();

        // 1. เริ่มเคลื่อนไหว Object
        if (scareObject != null)
        {
            scareObject.SetActive(true);
            StartCoroutine(FlyBySequence());
        }

        // 2. เล่นเสียง
        if (audioSource != null && scareSound != null)
        {
            audioSource.clip = scareSound;
            audioSource.volume = volumeScale;
            audioSource.Play();
        }

        // 3. เริ่มสั่นกล้อง
        Coroutine shakeCoroutine = null;
        if (enableCameraShake)
            shakeCoroutine = StartCoroutine(CameraShake());

        // 4. เริ่มแฟลชหน้าจอ
        if (enableScreenFlash)
            StartCoroutine(ScreenFlash());

        // 5. รอจน Event จบ
        yield return new WaitForSeconds(scareDuration);

        // 6. เคลียร์ทุกอย่าง
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        if (scareObject != null)
            scareObject.SetActive(false);

        if (disablePlayerControl)
            EnablePlayerControls();

        if (mainCamera != null)
            mainCamera.transform.localPosition = originalCameraPos;
    }

    /// <summary>
    /// Coroutine สำหรับควบคุมการเคลื่อนที่ของ Object (ตาม Enum)
    /// </summary>
    private IEnumerator FlyBySequence()
    {
        Transform camTransform = mainCamera.transform;

        Vector3 localStartOffset;
        Vector3 localEndOffset;

        // กำหนดค่าตาม Enum ที่เลือก
        switch (direction)
        {
            case JumpScareDirection.FlyTowardsFace:
                localStartOffset = new Vector3(0f, 0f, 10f); // ไกล
                localEndOffset = new Vector3(0f, 0f, 0.5f);  // ใกล้
                break;
            case JumpScareDirection.FlyFromLeft:
                localStartOffset = new Vector3(-5f, 0f, 3f); // ซ้าย
                localEndOffset = new Vector3(5f, 0f, 3f);   // ขวา
                break;
            case JumpScareDirection.FlyFromRight:
                localStartOffset = new Vector3(5f, 0f, 3f);   // ขวา
                localEndOffset = new Vector3(-5f, 0f, 3f); // ซ้าย
                break;
            case JumpScareDirection.FlyFromTop:
                localStartOffset = new Vector3(0f, 5f, 3f);   // บน
                localEndOffset = new Vector3(0f, -5f, 3f);  // ล่าง
                break;
            case JumpScareDirection.FlyFromBottom:
                localStartOffset = new Vector3(0f, -5f, 3f);  // ล่าง
                localEndOffset = new Vector3(0f, 5f, 3f);   // บน
                break;
            case JumpScareDirection.FlyPastFace:
                localStartOffset = new Vector3(0f, 0f, -5f);  // หลัง
                localEndOffset = new Vector3(0f, 0f, 5f);   // หน้า
                break;
            case JumpScareDirection.Custom:
            default:
                localStartOffset = this.startOffset; 
                localEndOffset = this.endOffset;
                break;
        }

        Vector3 worldStart = camTransform.TransformPoint(localStartOffset);
        Vector3 worldEnd = camTransform.TransformPoint(localEndOffset);

        scareObject.transform.position = worldStart;
        scareObject.transform.LookAt(camTransform.position);

        float elapsed = 0f;
        while (elapsed < flyByDuration)
        {
            float t_raw = elapsed / flyByDuration;
            float t_curved = flyByCurve.Evaluate(t_raw);

            scareObject.transform.position = Vector3.Lerp(worldStart, worldEnd, t_curved);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        scareObject.transform.position = worldEnd;
    }

    /// <summary>
    /// Coroutine สั่นกล้อง
    /// </summary>
    private IEnumerator CameraShake()
    {
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.localPosition;

        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            float intensity = shakeIntensity * (1f - progress); // ลดความแรงลงเรื่อยๆ

            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            mainCamera.transform.localPosition = startPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = startPos;
    }

    /// <summary>
    /// Coroutine แฟลชหน้าจอ
    /// </summary>
    private IEnumerator ScreenFlash()
    {
        isFlashing = true;
        float elapsed = 0f;
        float duration = 0.5f; // ระยะเวลาแฟลช

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            currentFlashAlpha = flashCurve.Evaluate(progress) * screenFlashIntensity;
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentFlashAlpha = 0f;
        isFlashing = false;
    }

    /// <summary>
    /// (Legacy UI) วาด Texture แฟลชทับหน้าจอ
    /// </summary>
    private void OnGUI()
    {
        if (!isFlashing || currentFlashAlpha <= 0f) return;

        Color color = flashColor;
        color.a = currentFlashAlpha;
        GUI.color = color;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTexture);
        GUI.color = Color.white;
    }

    /// <summary>
    /// ปิดการควบคุม Player
    /// </summary>
    private void DisablePlayerControls()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning("JumpScareManager: Player object not found!");
            return;
        }

        disabledScripts.Clear();
        MonoBehaviour[] allScripts = player.GetComponentsInChildren<MonoBehaviour>();

        foreach (var script in allScripts)
        {
            if (script == null || script == this) continue;

            string scriptType = script.GetType().Name;
            if (scriptsToDisable.Contains(scriptType))
            {
                disabledScripts[script] = script.enabled;
                script.enabled = false;
            }
        }

        // ปิด CharacterController ด้วย (ถ้ามี)
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;
    }

    /// <summary>
    /// เปิดการควบคุม Player กลับคืน
    /// </summary>
    private void EnablePlayerControls()
    {
        // เปิด Script คืน
        foreach (var kvp in disabledScripts)
        {
            if (kvp.Key != null)
                kvp.Key.enabled = kvp.Value; // คืนค่าเดิม (อาจจะเคยปิดอยู่แล้ว)
        }

        // เปิด CharacterController คืน
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = true;
        }

        disabledScripts.Clear();
    }

    /// <summary>
    /// ทำลาย Texture เมื่อ Object ถูกทำลาย
    /// </summary>
    private void OnDestroy()
    {
        if (flashTexture != null)
            Destroy(flashTexture);
    }

    /// <summary>
    /// วาด Gizmos ใน Scene View เพื่อให้เห็น 'เส้นทาง'
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null) return;

        Transform camTransform = mainCamera.transform;
        
        // เราต้องจำลองการทำงานของ Switch Case ใน FlyBySequence
        // เพื่อให้ Gizmos อัปเดตตาม Dropdown
        Vector3 localStartOffset;
        Vector3 localEndOffset;

        switch (direction)
        {
            case JumpScareDirection.FlyTowardsFace:
                localStartOffset = new Vector3(0f, 0f, 10f);
                localEndOffset = new Vector3(0f, 0f, 0.5f);
                break;
            case JumpScareDirection.FlyFromLeft:
                localStartOffset = new Vector3(-5f, 0f, 3f);
                localEndOffset = new Vector3(5f, 0f, 3f);
                break;
            case JumpScareDirection.FlyFromRight:
                localStartOffset = new Vector3(5f, 0f, 3f);
                localEndOffset = new Vector3(-5f, 0f, 3f);
                break;
            case JumpScareDirection.FlyFromTop:
                localStartOffset = new Vector3(0f, 5f, 3f);
                localEndOffset = new Vector3(0f, -5f, 3f);
                break;
            case JumpScareDirection.FlyFromBottom:
                localStartOffset = new Vector3(0f, -5f, 3f);
                localEndOffset = new Vector3(0f, 5f, 3f);
                break;
            case JumpScareDirection.FlyPastFace:
                localStartOffset = new Vector3(0f, 0f, -5f);
                localEndOffset = new Vector3(0f, 0f, 5f);
                break;
            case JumpScareDirection.Custom:
            default:
                localStartOffset = this.startOffset; 
                localEndOffset = this.endOffset;
                break;
        }

        Vector3 worldStart = camTransform.TransformPoint(localStartOffset);
        Vector3 worldEnd = camTransform.TransformPoint(localEndOffset);

        // วาดจุดเริ่ม (เขียว) และจุดจบ (แดง)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(worldStart, 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(worldEnd, 0.3f);

        // วาดเส้นทาง
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldStart, worldEnd);
    }
}