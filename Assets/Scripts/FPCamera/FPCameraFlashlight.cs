using UnityEngine;
using UnityEngine.InputSystem;

public class FPCameraFlashlight : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("If true, moving the mouse up looks up. If false, moving the mouse up looks down.")]
    [SerializeField] private bool invertY = true;

    [Header("Sensitivity Settings")]
    [Tooltip("Horizontal mouse sensitivity (frame-rate independent).")]
    [SerializeField] private float sensX = 100f;
    [Tooltip("Vertical mouse sensitivity (frame-rate independent).")]
    [SerializeField] private float sensY = 100f;

    [Header("Horizontal Rotation Limits")]
    [Tooltip("Maximum degrees to rotate left from starting orientation.")]
    [SerializeField] private float maxLeftRot = 30f;
    [Tooltip("Maximum degrees to rotate right from starting orientation.")]
    [SerializeField] private float maxRightRot = 30f;

    [Header("Vertical Rotation Limits")]
    [Tooltip("Maximum degrees to rotate up from starting orientation.")]
    [SerializeField] private float maxUpRot = 30f;
    [Tooltip("Maximum degrees to rotate down from starting orientation.")]
    [SerializeField] private float maxDownRot = 30f;

    [Header("Return to Center")]
    [Tooltip("Speed at which the flashlight returns to center when not moving.")]
    [SerializeField] private float returnSpeed = 10f;
    [Tooltip("Minimum input magnitude before applying return to center.")]
    [SerializeField] private float inputThreshold = 0.001f;

    private float currentYaw;
    private float currentPitch;
    private Vector3 startRotation;
    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable() => controls.Enable();

    private void OnDisable() => controls.Disable();

    private void Start()
    {
        Vector3 localEuler = transform.localEulerAngles;
        startRotation = new Vector3(localEuler.x, localEuler.y, localEuler.z);
        currentYaw = 0f;
        currentPitch = 0f;
    }

    private void Update()
    {
        Vector2 lookInput = controls.Player.Look.ReadValue<Vector2>();
        
        if (HasSignificantInput(lookInput))
        {
            ApplyLookRotation(lookInput);
        }
        else
        {
            ReturnToCenter();
        }

        ApplyFinalRotation();
    }

    private bool HasSignificantInput(Vector2 input)
    {
        return input.sqrMagnitude >= inputThreshold;
    }

    private void ApplyLookRotation(Vector2 input)
    {
        float deltaTime = Time.deltaTime;
        float mouseX = input.x * sensX * deltaTime;
        float mouseY = input.y * sensY * deltaTime;

        currentYaw = Mathf.Clamp(currentYaw + mouseX, -maxLeftRot, maxRightRot);
        
        float pitchDelta = invertY ? -mouseY : mouseY;
        currentPitch = Mathf.Clamp(currentPitch + pitchDelta, -maxDownRot, maxUpRot);
    }

    private void ReturnToCenter()
    {
        float returnDelta = Time.deltaTime * returnSpeed;
        currentYaw = Mathf.MoveTowards(currentYaw, 0f, returnDelta);
        currentPitch = Mathf.MoveTowards(currentPitch, 0f, returnDelta);
    }

    private void ApplyFinalRotation()
    {
        transform.localRotation = Quaternion.Euler(
            startRotation.x + currentPitch,
            startRotation.y + currentYaw,
            startRotation.z
        );
    }
}