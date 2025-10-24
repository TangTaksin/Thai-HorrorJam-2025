using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwayTilt : MonoBehaviour
{
    [Header("Mouse Sway Settings")]
    [SerializeField] private float mouseSwayAmount = 2f;
    [SerializeField] private float mouseSwaySmooth = 8f;
    [SerializeField] private float mouseSwayMaxSpeed = 15f;
    [SerializeField] private bool enableSway = true;

    [Header("Movement Tilt Settings")]
    [SerializeField] private float moveTiltAmount = 3f;
    [SerializeField] private float moveTiltSmooth = 8f;
    [SerializeField] private bool enableTilt = true;

    [Header("Manual Head Tilt Settings")]
    [SerializeField] private float headTiltAmount = 15f;
    [SerializeField] private float headTiltSmooth = 10f;
    [SerializeField] private float headPeekDistance = 0.5f;
    [SerializeField] private float peekReturnSpeed = 12f;
    [SerializeField] private bool enableHeadTilt = true;

    [Header("Wall Collision Prevention")]
    [SerializeField] private float wallCheckDistance = 0.4f;
    [SerializeField] private float wallCheckInterval = 0.1f;
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private float wallReductionFactor = 0.25f;

    [Header("Advanced Smoothing")]
    [SerializeField] private bool useAdaptiveSmoothing = true;
    [SerializeField] private AnimationCurve swayResponseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float velocityDamping = 0.95f;

    private Quaternion initialRotation;
    private Quaternion currentSway;
    private Quaternion currentTilt;
    private Quaternion currentHeadTilt;

    private Vector3 initialPosition;
    private Vector3 currentPeekOffset;
    private Vector3 peekVelocity;

    private PlayerControls controls;
    private float tiltInput;
    private float smoothTiltInput;

    private Vector2 lookInput;
    private Vector2 smoothLookInput;
    private Vector2 lookVelocity;
    
    private Vector2 moveInput;
    private Vector2 smoothMoveInput;

    private bool isNearWall;
    private float lastWallCheckTime;
    private float wallInfluence;

    private void Awake()
    {
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
        currentSway = Quaternion.identity;
        currentTilt = Quaternion.identity;
        currentHeadTilt = Quaternion.identity;

        // Initialize Input System
        controls = new PlayerControls();

        // Mouse / Gamepad Look with smooth input
        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        // Movement input
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Head Tilt - improved to handle simultaneous inputs
        controls.Player.HeadTiltLeft.performed += ctx => UpdateTiltInput(-1f);
        controls.Player.HeadTiltLeft.canceled += ctx => UpdateTiltInput(0f);
        controls.Player.HeadTiltRight.performed += ctx => UpdateTiltInput(1f);
        controls.Player.HeadTiltRight.canceled += ctx => UpdateTiltInput(0f);
    }

    private void UpdateTiltInput(float value)
    {
        // Handle when both keys might be pressed
        if (value == 0f)
        {
            // Check if opposite key is still held
            if (controls.Player.HeadTiltLeft.IsPressed()) tiltInput = -1f;
            else if (controls.Player.HeadTiltRight.IsPressed()) tiltInput = 1f;
            else tiltInput = 0f;
        }
        else
        {
            tiltInput = value;
        }
    }

    private void OnEnable() => controls?.Enable();
    private void OnDisable() => controls?.Disable();

    private void Update()
    {
        CheckWallProximity();
        SmoothInputs();
        HandleSwayAndTilt();
    }

    private void CheckWallProximity()
    {
        // Optimize: Only check walls at intervals
        if (Time.time - lastWallCheckTime < wallCheckInterval)
            return;

        lastWallCheckTime = Time.time;
        isNearWall = PerformWallCheck();

        // Smooth transition of wall influence
        float targetInfluence = isNearWall ? wallReductionFactor : 1f;
        wallInfluence = Mathf.Lerp(wallInfluence, targetInfluence, Time.deltaTime * 8f);
    }

    private bool PerformWallCheck()
    {
        // Optimized: Check only left/right sides when tilting
        if (Mathf.Abs(tiltInput) < 0.1f)
            return false;

        Vector3 origin = transform.position;
        Vector3 sideDir = transform.right * Mathf.Sign(tiltInput);
        
        return Physics.SphereCast(origin, 0.1f, sideDir, out _, wallCheckDistance, wallLayers);
    }

    private void SmoothInputs()
    {
        // Smooth look input for more natural sway
        if (useAdaptiveSmoothing)
        {
            float lookMagnitude = lookInput.magnitude;
            float responseCurveValue = swayResponseCurve.Evaluate(Mathf.Clamp01(lookMagnitude / mouseSwayMaxSpeed));
            
            smoothLookInput = Vector2.SmoothDamp(
                smoothLookInput, 
                lookInput * responseCurveValue, 
                ref lookVelocity, 
                1f / mouseSwaySmooth
            );
        }
        else
        {
            smoothLookInput = Vector2.Lerp(smoothLookInput, lookInput, Time.deltaTime * mouseSwaySmooth);
        }

        // Smooth movement input
        smoothMoveInput = Vector2.Lerp(smoothMoveInput, moveInput, Time.deltaTime * moveTiltSmooth);

        // Smooth tilt input with easing
        float tiltTarget = tiltInput * wallInfluence;
        smoothTiltInput = Mathf.Lerp(smoothTiltInput, tiltTarget, Time.deltaTime * headTiltSmooth);
    }

    private void HandleSwayAndTilt()
    {
        // --- Mouse / Gamepad Sway ---
        if (enableSway)
        {
            // Clamp look input to prevent excessive sway
            Vector2 clampedLook = Vector2.ClampMagnitude(smoothLookInput, mouseSwayMaxSpeed);
            
            Quaternion targetSway = Quaternion.Euler(
                -clampedLook.y * mouseSwayAmount,
                clampedLook.x * mouseSwayAmount,
                0f
            );
            
            currentSway = Quaternion.Slerp(currentSway, targetSway, Time.deltaTime * mouseSwaySmooth);
        }
        else
        {
            currentSway = Quaternion.Slerp(currentSway, Quaternion.identity, Time.deltaTime * mouseSwaySmooth);
        }

        // --- Movement Tilt ---
        if (enableTilt)
        {
            // Add subtle forward/back pitch based on movement
            float forwardTilt = -smoothMoveInput.y * moveTiltAmount * 0.3f;
            
            Quaternion targetTilt = Quaternion.Euler(
                forwardTilt,
                0f,
                -smoothMoveInput.x * moveTiltAmount
            );
            
            currentTilt = Quaternion.Slerp(currentTilt, targetTilt, Time.deltaTime * moveTiltSmooth);
        }
        else
        {
            currentTilt = Quaternion.Slerp(currentTilt, Quaternion.identity, Time.deltaTime * moveTiltSmooth);
        }

        // --- Manual Head Tilt & Peek ---
        if (enableHeadTilt)
        {
            float effectiveTilt = smoothTiltInput * headTiltAmount;
            float effectivePeek = smoothTiltInput * headPeekDistance * wallInfluence;

            // Rotation (roll)
            Quaternion targetHeadTilt = Quaternion.Euler(0f, 0f, -effectiveTilt);
            currentHeadTilt = Quaternion.Slerp(currentHeadTilt, targetHeadTilt, Time.deltaTime * headTiltSmooth);

            // Position offset with SmoothDamp for better feel
            Vector3 targetPeek = new Vector3(effectivePeek, 0f, 0f);
            float smoothSpeed = Mathf.Abs(tiltInput) > 0.1f ? headTiltSmooth : peekReturnSpeed;
            currentPeekOffset = Vector3.SmoothDamp(
                currentPeekOffset, 
                targetPeek, 
                ref peekVelocity, 
                1f / smoothSpeed
            );
        }
        else
        {
            currentHeadTilt = Quaternion.Slerp(currentHeadTilt, Quaternion.identity, Time.deltaTime * headTiltSmooth);
            currentPeekOffset = Vector3.SmoothDamp(
                currentPeekOffset, 
                Vector3.zero, 
                ref peekVelocity, 
                1f / peekReturnSpeed
            );
        }

        // Apply velocity damping for micro-smoothing
        peekVelocity *= velocityDamping;
        lookVelocity *= velocityDamping;

        // Final application
        transform.localRotation = initialRotation * currentSway * currentTilt * currentHeadTilt;
        transform.localPosition = initialPosition + currentPeekOffset;
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = isNearWall ? Color.red : Color.green;
        
        // Draw wall check for left/right side only
        if (Mathf.Abs(tiltInput) > 0.1f)
        {
            Vector3 sideDir = transform.right * Mathf.Sign(tiltInput);
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            Gizmos.DrawLine(transform.position, transform.position + sideDir * wallCheckDistance);
        }
    }
}