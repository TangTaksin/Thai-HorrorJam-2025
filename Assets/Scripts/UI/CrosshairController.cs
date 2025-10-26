using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CrosshairLayerSetting
{
    [Tooltip("Layer ที่ต้องการให้ Crosshair ตอบสนอง")]
    public LayerMask layer;
    
    [Tooltip("สีที่จะเปลี่ยน")]
    public Color color = Color.green;
    
    [Tooltip("ขนาดที่จะเปลี่ยน")]
    public float size = 10f;
    
    [Tooltip("Icon ที่จะเปลี่ยน (ไม่บังคับ)")]
    public Sprite icon;
    
    [Tooltip("ระยะห่างสูงสุดสำหรับ Layer นี้ (เมตร)")]
    public float maxDistance = 2f;
}

public class CrosshairController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image crosshair;

    [Header("Default Settings")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private float defaultSize = 15f;
    [SerializeField] private Sprite defaultIcon;

    [Header("Running Settings")]
    [SerializeField] private Color runningColor = Color.red;
    [SerializeField] private float runSize = 25f;
    [SerializeField] private Sprite runningIcon;

    [Header("Layer-Specific Settings")]
    [Tooltip("รายการตั้งค่า Crosshair ตาม Layer")]
    [SerializeField] private CrosshairLayerSetting[] layerSettings;

    [Header("Player Reference")]
    [SerializeField] private FPPlayerController player;
    [SerializeField] private FPInteract interact;

    private RectTransform rectTransform;
    private Color currentTargetColor;
    private Sprite currentTargetIcon;

    private const float SIZE_LERP_SPEED = 10f;
    private const float MIN_RUNNING_SPEED_SQR = 0.01f;

    void Start()
    {
        InitializeCrosshair();
        ValidateReferences();
        SetupCursor();
    }

    void Update()
    {
        if (!IsValid()) return;

        DetermineCrosshairState(out Color targetColor, out float targetSize, out Sprite targetIcon);
        UpdateCrosshairAppearance(targetColor, targetSize, targetIcon);
    }

    private void InitializeCrosshair()
    {
        if (crosshair == null)
            crosshair = GetComponent<Image>();

        rectTransform = crosshair.GetComponent<RectTransform>();
        currentTargetColor = defaultColor;
        crosshair.color = defaultColor;

        if (defaultIcon != null)
        {
            currentTargetIcon = defaultIcon;
            crosshair.sprite = defaultIcon;
        }
        else
        {
            Debug.LogError("CrosshairController: 'Default Icon' is not set!");
        }
    }

    private void ValidateReferences()
    {
        if (player == null)
            Debug.LogWarning("CrosshairController: Player (FPPlayerController) reference not set!");
        
        if (interact == null)
            Debug.LogWarning("CrosshairController: Interact (FPInteract) reference not set!");
    }

    private void SetupCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private bool IsValid()
    {
        return player != null && interact != null && player.Controller != null;
    }

    private void DetermineCrosshairState(out Color targetColor, out float targetSize, out Sprite targetIcon)
    {
        targetColor = defaultColor;
        targetSize = defaultSize;
        targetIcon = defaultIcon;

        GameObject detectedObject = interact.GetDetectedObject();

        if (detectedObject != null)
        {
            CrosshairLayerSetting setting = GetSettingForLayer(detectedObject.layer);

            if (setting != null)
            {
                targetColor = setting.color;
                targetSize = setting.size;
                targetIcon = setting.icon != null ? setting.icon : defaultIcon;
                return;
            }
        }

        if (IsPlayerRunning())
        {
            targetColor = runningColor;
            targetSize = runSize;
            targetIcon = runningIcon != null ? runningIcon : defaultIcon;
        }
    }

    private bool IsPlayerRunning()
    {
        Vector3 velocity = player.Controller.velocity;
        float horizontalSpeedSqr = velocity.x * velocity.x + velocity.z * velocity.z;
        return Input.GetKey(KeyCode.LeftShift) && horizontalSpeedSqr > MIN_RUNNING_SPEED_SQR;
    }

    private void UpdateCrosshairAppearance(Color targetColor, float targetSize, Sprite targetIcon)
    {
        rectTransform.sizeDelta = Vector2.Lerp(
            rectTransform.sizeDelta,
            Vector2.one * targetSize,
            Time.deltaTime * SIZE_LERP_SPEED
        );

        if (currentTargetColor != targetColor)
        {
            crosshair.color = targetColor;
            currentTargetColor = targetColor;
        }

        if (currentTargetIcon != targetIcon)
        {
            crosshair.sprite = targetIcon;
            currentTargetIcon = targetIcon;
        }
    }

    /// <summary>
    /// ค้นหา Setting ที่ตรงกับ Layer ที่ระบุ
    /// </summary>
    /// <param name="objectLayer">Layer ของ Object ที่ตรวจจับ</param>
    /// <returns>CrosshairLayerSetting ถ้าเจอ, หรือ null ถ้าไม่เจอ</returns>
    public CrosshairLayerSetting GetSettingForLayer(int objectLayer)
    {
        foreach (CrosshairLayerSetting setting in layerSettings)
        {
            if (setting.layer == (setting.layer | (1 << objectLayer)))
            {
                return setting;
            }
        }

        return null;
    }
}