using UnityEngine;
using UnityEngine.InputSystem;

public class FPCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform handHolder;
    [SerializeField] private Transform flashlight;

    [Header("Look Settings")]
    [SerializeField, Range(0.1f, 10f)] private float lookSensitivity = 1f;
    [SerializeField, Range(1f, 20f)] private float lookSmoothing = 8f;
    [SerializeField] private bool smoothLook = true;
    [SerializeField, Range(10f, 90f)] private float verticalLookLimit = 80f;

    [Header("Follow Settings")]
    [SerializeField, Range(1f, 50f)] private float handFollowSpeed = 15f;
    [SerializeField, Range(1f, 50f)] private float flashlightFollowSpeed = 12f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeAmplitude = 0.15f;
    [SerializeField] private float shakeFrequency = 1f;
    [SerializeField] private float shakeSmoothing = 4f;

    // Rotation state
    private float verticalRotation;
    private float horizontalRotation;
    private float targetVerticalRotation;
    private float targetHorizontalRotation;

    // Input
    bool canInput = true;
    private Vector2 lookInput;

    // Camera shake
    private Vector2 currentShake;
    private Vector2 shakeVelocity;
    private Vector2 shakeOffset;
    private float shakeDampTime;

    // Cached rotation
    private Quaternion baseCameraRotation;

    private PlayerControls controls;

    // Public property for sensitivity control
    public float MouseSensitivity
    {
        get => lookSensitivity;
        set => lookSensitivity = Mathf.Max(0.1f, value);
    }

    private void Awake()
    {
        InitializeCamera();
        InitializeCursor();
        InitializeShake();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();

        controls.Player.Look.performed += OnLookPerformed;
        controls.Player.Look.canceled += OnLookCanceled;

        UISettingsManager.OnSettingApplied += ApplySetting;
    }

    private void OnDisable()
    {
        controls.Player.Look.performed -= OnLookPerformed;
        controls.Player.Look.canceled -= OnLookCanceled;

        UISettingsManager.OnSettingApplied -= ApplySetting;

        controls.Disable();
    }

    public void ApplySetting()
    {
        lookSensitivity = PlayerPrefs.GetFloat("LookSensitivity");
        smoothLook = PlayerPrefs.GetInt("CameraSmooth", 1) == 1;
    }

    public void HandlePause(bool isPlaying)
    {
        canInput = isPlaying;
    }

    private void InitializeCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main?.transform;

        if (mainCamera == null)
            Debug.LogWarning($"{nameof(FPCameraController)}: Main Camera not assigned or found!");
    }

    private void InitializeCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void InitializeShake()
    {
        shakeOffset = new Vector2(Random.Range(0f, 100f), Random.Range(0f, 100f));
        shakeDampTime = 1f / shakeSmoothing;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        if (canInput)
            lookInput = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }

    void Start()
    {
        ResetView();
    }

    private void Update()
    {
        ApplyLookRotation();
    }

    private void FixedUpdate()
    {
        UpdateCameraShake();
    }

    private void LateUpdate()
    {
        ApplyCameraShake();
        UpdateFollowTransforms();
    }

    private void ApplyLookRotation()
    {
        if (playerBody == null || mainCamera == null) return;

        // Clamp mouse input to avoid jitter from sudden spikes
        Vector2 clampedInput = Vector2.ClampMagnitude(lookInput, 20f);

        // Update target rotation
        targetHorizontalRotation += clampedInput.x * lookSensitivity;
        targetVerticalRotation = Mathf.Clamp(
            targetVerticalRotation - clampedInput.y * lookSensitivity,
            -verticalLookLimit,
            verticalLookLimit
        );

        if (!smoothLook)
        {
            horizontalRotation = targetHorizontalRotation;
            verticalRotation = targetVerticalRotation;
        }
        else
        {
            // Exponential smoothing (frame-rate independent)
            float dt = Time.deltaTime;
            float smoothFactor = 1f - Mathf.Exp(-lookSmoothing * dt);

            horizontalRotation = Mathf.Lerp(horizontalRotation, targetHorizontalRotation, smoothFactor);
            verticalRotation = Mathf.Lerp(verticalRotation, targetVerticalRotation, smoothFactor);
        }

        // Apply base rotations
        playerBody.localRotation = Quaternion.Euler(0f, horizontalRotation, 0f);
        baseCameraRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void UpdateCameraShake()
    {
        float time = Time.time * shakeFrequency;
        float targetX = (Mathf.PerlinNoise(time + shakeOffset.x, 0f) - 0.5f) * 2f * shakeAmplitude;
        float targetY = (Mathf.PerlinNoise(0f, time + shakeOffset.y) - 0.5f) * 2f * shakeAmplitude;

        // Smooth damp for organic shake
        float dt = Time.fixedDeltaTime;
        currentShake.x = Mathf.SmoothDamp(currentShake.x, targetX, ref shakeVelocity.x, shakeDampTime, Mathf.Infinity, dt);
        currentShake.y = Mathf.SmoothDamp(currentShake.y, targetY, ref shakeVelocity.y, shakeDampTime, Mathf.Infinity, dt);
    }

    private void ApplyCameraShake()
    {
        if (mainCamera == null) return;

        // Combine base rotation with shake offset
        Quaternion shakeRotation = Quaternion.Euler(currentShake.y, currentShake.x, 0f);
        mainCamera.localRotation = baseCameraRotation * shakeRotation;
    }

    private void UpdateFollowTransforms()
    {
        if (mainCamera == null) return;

        float dt = Time.deltaTime;

        // Hand follows camera (local space)
        if (handHolder != null)
        {
            float handSmooth = 1f - Mathf.Exp(-handFollowSpeed * dt);
            handHolder.localRotation = Quaternion.LerpUnclamped(
                handHolder.localRotation,
                mainCamera.localRotation,
                handSmooth
            );
        }

        // Flashlight follows camera (world space)
        if (flashlight != null)
        {
            float flashSmooth = 1f - Mathf.Exp(-flashlightFollowSpeed * dt);
            flashlight.rotation = Quaternion.LerpUnclamped(
                flashlight.rotation,
                mainCamera.rotation,
                flashSmooth
            );
        }
    }

    /// <summary>
    /// Resets camera view to a specific yaw/pitch (useful for respawn or cutscenes).
    /// </summary>
    public void ResetView(float yaw = 0f, float pitch = 0f)
    {
        horizontalRotation = targetHorizontalRotation = yaw;
        verticalRotation = targetVerticalRotation = pitch;

        if (playerBody != null)
            playerBody.localRotation = Quaternion.Euler(0f, yaw, 0f);

        if (mainCamera != null)
            mainCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
