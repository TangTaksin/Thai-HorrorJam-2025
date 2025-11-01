using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuJS : MonoBehaviour
{
    [Header("Jump Scare Settings")]
    [SerializeField] private GameObject scareObject;
    [SerializeField] private Transform scarePosition;
    [SerializeField] private AudioClip scareSound;
    [SerializeField] private float scareDuration = 2f;
    [SerializeField] private bool disablePlayerControl = true;

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

    // 🟢 1. เพิ่ม 2 บรรทัดนี้
    [Header("Key Trigger (Optional)")]
    [SerializeField] private bool enableKeyTrigger = false; // ติ๊กถูกเพื่อเปิดใช้งานการกดปุ่ม
    [SerializeField] private KeyCode triggerKey = KeyCode.J; // ปุ่มที่จะใช้กด (เลือกได้ใน Inspector)

    [Header("Player Control (Specific Scripts)")]
    [SerializeField]
    private List<string> scriptsToDisable = new List<string>
    {
        "FirstPersonController",
        "PlayerMovement",
        "MouseLook"
    };

    private Camera mainCamera;
    private bool hasTriggered = false;
    private Vector3 originalCameraPos;
    private Dictionary<MonoBehaviour, bool> disabledScripts = new Dictionary<MonoBehaviour, bool>();
    private Texture2D flashTexture;
    private bool isFlashing = false;
    private float currentFlashAlpha = 0f;

    private void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("JumpScareManager: Main Camera not found!");
            enabled = false;
            return;
        }

        if (scareObject != null)
            scareObject.SetActive(false);

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

        // Create flash texture
        CreateFlashTexture();
    }

    // 🟢 2. เพิ่มฟังก์ชัน Update() นี้
    private void Update()
    {
        // ถ้าเราเปิดใช้งานการกดปุ่ม และ ปุ่มที่กำหนดถูกกดลง
        if (enableKeyTrigger && Input.GetKeyDown(triggerKey))
        {
            Debug.Log($"Key {triggerKey} pressed, triggering jumpscare!");
            TriggerJumpScare();
        }
    }

    private void CreateFlashTexture()
    {
        flashTexture = new Texture2D(1, 1);
        flashTexture.SetPixel(0, 0, Color.white);
        flashTexture.Apply();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;

        // Trigger จาก Collider (ของเดิม)
        TriggerJumpScare();
    }

    public void TriggerJumpScare()
    {
        if (hasTriggered && triggerOnce) return;

        hasTriggered = true;
        StartCoroutine(JumpScareSequence());
    }

    private IEnumerator JumpScareSequence()
    {
        // Store original camera position (local to handle parented cameras)
        originalCameraPos = mainCamera.transform.localPosition;

        // Disable player control
        if (disablePlayerControl)
            DisablePlayerControls();

        // Show scare object
        if (scareObject != null)
        {
            PositionScareObject();
            scareObject.SetActive(true);
        }

        // Play sound
        if (audioSource != null && scareSound != null)
        {
            audioSource.clip = scareSound;
            audioSource.volume = volumeScale;
            audioSource.Play();
        }

        // Camera shake
        Coroutine shakeCoroutine = null;
        if (enableCameraShake)
            shakeCoroutine = StartCoroutine(CameraShake());

        // Screen flash
        if (enableScreenFlash)
            StartCoroutine(ScreenFlash());

        // Wait for scare duration
        yield return new WaitForSeconds(scareDuration);

        // Ensure shake is complete
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        // Hide scare object
        if (scareObject != null)
            scareObject.SetActive(false);

        // Re-enable player control
        if (disablePlayerControl)
            EnablePlayerControls();

        // Reset camera position
        if (mainCamera != null)
            mainCamera.transform.localPosition = originalCameraPos;
    }

    private void PositionScareObject()
    {
        if (scarePosition != null)
        {
            scareObject.transform.position = scarePosition.position;
            scareObject.transform.rotation = scarePosition.rotation;
        }
        else
        {
            // Position in front of camera
            scareObject.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 2f;
            scareObject.transform.LookAt(mainCamera.transform);
        }
    }

    private IEnumerator CameraShake()
    {
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.localPosition;

        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            float intensity = shakeIntensity * (1f - progress); // Decrease over time

            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            mainCamera.transform.localPosition = startPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we return to exact original position
        mainCamera.transform.localPosition = startPos;
    }

    private IEnumerator ScreenFlash()
    {
        isFlashing = true;
        float elapsed = 0f;
        float duration = 0.5f;

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

    private void OnGUI()
    {
        if (!isFlashing || currentFlashAlpha <= 0f) return;

        Color color = flashColor;
        color.a = currentFlashAlpha;
        GUI.color = color;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTexture);
        GUI.color = Color.white;
    }

    private void DisablePlayerControls()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning("JumpScareManager: Player object not found!");
            return;
        }

        disabledScripts.Clear();

        // Get all MonoBehaviours on player and children
        MonoBehaviour[] allScripts = player.GetComponentsInChildren<MonoBehaviour>();

        foreach (var script in allScripts)
        {
            if (script == null || script == this) continue;

            // Check if this script should be disabled
            string scriptType = script.GetType().Name;
            if (scriptsToDisable.Contains(scriptType))
            {
                disabledScripts[script] = script.enabled;
                script.enabled = false;
            }
        }

        // Also try to disable CharacterController velocity
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;
    }

    private void EnablePlayerControls()
    {
        // Re-enable scripts in reverse order
        foreach (var kvp in disabledScripts)
        {
            if (kvp.Key != null)
                kvp.Key.enabled = kvp.Value; // Restore original state
        }

        // Re-enable CharacterController
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = true;
        }

        disabledScripts.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (scarePosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(scarePosition.position, 0.5f);
            Gizmos.DrawLine(transform.position, scarePosition.position);

            // Draw forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(scarePosition.position, scarePosition.forward * 2f);
        }
    }

    private void OnDestroy()
    {
        if (flashTexture != null)
            Destroy(flashTexture);
    }

    // Public method to reset the trigger
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
