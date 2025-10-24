using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image crosshair;

    [Header("Settings")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color runningColor = Color.red;
    [SerializeField] private float defaultSize = 15f;
    [SerializeField] private float runSize = 25f;

    [Header("Player Reference")]
    [SerializeField] private FPPlayerController player;

    private RectTransform rectTransform;

    void Start()
    {
        if (crosshair == null) crosshair = GetComponent<Image>();
        rectTransform = crosshair.GetComponent<RectTransform>();
        crosshair.color = defaultColor;

        if (player == null)
            Debug.LogWarning("FPSCrosshair: Player reference not set!");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (player == null) return;

        // Example: change crosshair size/color when running
        bool isRunning = Input.GetKey(KeyCode.LeftShift)&&
                         new Vector2(player.Controller.velocity.x, player.Controller.velocity.z).magnitude > 0.1f;

        rectTransform.sizeDelta = Vector2.Lerp(rectTransform.sizeDelta,
            Vector2.one * (isRunning ? runSize : defaultSize), Time.deltaTime * 10f);
        crosshair.color = isRunning ? runningColor : defaultColor;
    }
}
