using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraFOV : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPPlayerController player;

    [Header("FOV Settings")]
    [SerializeField, Range(40f, 120f)] private float baseFOV = 70f;
    [SerializeField, Range(1f, 20f)] private float sprintFOVIncrease = 10f;
    [SerializeField, Range(1f, 20f)] private float sprintFOVSpeed = 8f;

    [Header("Zoom Settings")]
    [SerializeField, Range(10f, 60f)] private float zoomFOV = 30f;
    [SerializeField, Range(1f, 20f)] private float zoomSpeed = 5f;

    [SerializeField] private Camera cam;
    private float currentTargetFOV;
    private PlayerControls controls;

    private bool isZooming;
    private bool isSprinting;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (player == null)
            player = GetComponentInParent<FPPlayerController>();

        cam.fieldOfView = baseFOV;
        currentTargetFOV = baseFOV;

        controls = new PlayerControls();
        controls.Player.Zoom.performed += ctx => isZooming = true;
        controls.Player.Zoom.canceled += ctx => isZooming = false;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        if (player == null) return;

        UpdateTargetFOV();
        SmoothFOVTransition();
    }

    private void UpdateTargetFOV()
    {
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
        bool moving = moveInput.sqrMagnitude > 0.01f;

        if (isZooming)
        {
            currentTargetFOV = zoomFOV;
        }
        else if (isSprinting && !player.IsCrouching && moving)
        {
            currentTargetFOV = baseFOV + sprintFOVIncrease;
        }
        else
        {
            currentTargetFOV = baseFOV;
        }
    }

    private void SmoothFOVTransition()
    {
        float lerpSpeed = isZooming ? zoomSpeed : sprintFOVSpeed;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, currentTargetFOV, Time.deltaTime * lerpSpeed);
    }
}
