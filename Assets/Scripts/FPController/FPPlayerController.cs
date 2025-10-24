using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPPlayerController : MonoBehaviour
{
    #region Serialized Fields
    [Header("Movement Settings")]
    [SerializeField] private bool enableMovement = true;
    [SerializeField, Range(1f, 10f)] private float walkSpeed = 3f;
    [SerializeField, Range(2f, 20f)] private float runSpeed = 6f;
    [SerializeField, Range(0.5f, 5f)] private float crouchWalkSpeed = 1.5f;

    [Header("Footstep Settings")]
    [SerializeField] private FootstepAudioEvent footstepEvent;

    [Header("Jump Settings")]
    [SerializeField] private bool enableJump = true;
    [SerializeField, Range(0f, 20f)] private float jumpForce = 6f;
    [SerializeField, Range(5f, 50f)] private float gravity = 20f;
    [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.1f;
    [SerializeField] private AudioEvent jumpSound;

    [Header("Crouch Settings")]
    [SerializeField] private bool enableCrouch = true;
    [SerializeField, Range(0.5f, 2f)] private float crouchHeight = 1f;
    [SerializeField, Range(5f, 20f)] private float crouchSpeed = 10f;
    [SerializeField] private CrouchMode crouchMode = CrouchMode.Hold;

    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform groundCheck;

    [Header("Ground Check Settings")]
    [SerializeField, Range(0.1f, 1f)] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    #endregion

    #region Private Fields
    private CharacterController controller;
    private PlayerControls controls;
    private Vector3 moveDir;

    // Height management
    private float defaultHeight;
    private float targetHeight;
    private float currentHeight;

    // State tracking
    private bool isGrounded;
    private float lastGroundedTime;
    private float stepTimer;
    private SurfaceType currentSurface = SurfaceType.Concrete;

    // Input cache
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool sprintHeld;
    private bool crouchHeld;

    // Optimization - cached collider array
    private Collider[] surfaceColliders = new Collider[1];
    #endregion

    #region Properties
    public bool IsCrouching => currentHeight < defaultHeight - 0.01f;
    public CharacterController Controller => controller;
    private bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        EnableControls();
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    private void Update()
    {
        UpdateGroundState();
        UpdateCrouch();
        UpdateJump();
        UpdateGravity();
        UpdateMovement();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        defaultHeight = controller.height;
        currentHeight = targetHeight = defaultHeight;
        lastGroundedTime = Time.time;

        controls = new PlayerControls();
    }

    private void EnableControls()
    {
        controls.Enable();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += _ => moveInput = Vector2.zero;
        controls.Player.Jump.performed += _ => jumpPressed = true;
        controls.Player.Sprint.performed += OnSprintPressed;
        controls.Player.Sprint.canceled += _ => sprintHeld = false;
        controls.Player.Crouch.performed += OnCrouchPressed;
        controls.Player.Crouch.canceled += OnCrouchReleased;
    }
    #endregion

    #region Input Handlers
    private void OnSprintPressed(InputAction.CallbackContext _)
    {
        sprintHeld = true;

        if (!enableCrouch) return;

        // Cancel crouch when sprinting
        if (crouchMode == CrouchMode.Hold && crouchHeld)
            crouchHeld = false;
        else if (crouchMode == CrouchMode.Toggle && IsCrouching)
            crouchHeld = false;
    }

    private void OnCrouchPressed(InputAction.CallbackContext _)
    {
        if (crouchMode == CrouchMode.Hold)
            crouchHeld = true;
        else if (crouchMode == CrouchMode.Toggle)
            crouchHeld = !crouchHeld;
    }

    private void OnCrouchReleased(InputAction.CallbackContext _)
    {
        if (crouchMode == CrouchMode.Hold)
            crouchHeld = false;
    }
    #endregion

    #region Ground Check
    private void UpdateGroundState()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
            lastGroundedTime = Time.time;
    }

    private bool CanCoyoteJump()
    {
        return Time.time - lastGroundedTime <= coyoteTime;
    }
    #endregion

    #region Movement
    private void UpdateMovement()
    {
        if (!CanMove()) return;

        Vector3 horizontalMove = CalculateMovement();
        ApplyMovement(horizontalMove);
        UpdateFootsteps(horizontalMove.magnitude);
    }

    private bool CanMove()
    {
        return enableMovement
            && controller != null
            && controller.enabled
            && controller.gameObject.activeInHierarchy;
    }

    private Vector3 CalculateMovement()
    {
        float speed = GetCurrentSpeed();
        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        return move * speed;
    }

    private float GetCurrentSpeed()
    {
        if (IsCrouching) return crouchWalkSpeed;
        return sprintHeld ? runSpeed : walkSpeed;
    }

    private void ApplyMovement(Vector3 horizontalMove)
    {
        moveDir.x = horizontalMove.x;
        moveDir.z = horizontalMove.z;
        controller.Move(moveDir * Time.deltaTime);
    }
    #endregion

    #region Footsteps
    private void UpdateFootsteps(float moveMagnitude)
    {
        if (!ShouldPlayFootstep(moveMagnitude))
        {
            stepTimer = 0f;
            return;
        }

        stepTimer += Time.deltaTime;


        stepTimer = 0f;
        PlayFootstep();

    }

    private bool ShouldPlayFootstep(float moveMagnitude)
    {
        return moveMagnitude > 0.1f && isGrounded && footstepEvent != null;
    }
    private void PlayFootstep()
    {
        DetectSurface();

        if (SOAudioManager.Instance?.sfxSource != null)
        {
            footstepEvent.PlayOneShot(SOAudioManager.Instance.sfxSource, currentSurface);
        }
    }

    private void DetectSurface()
    {
        int count = Physics.OverlapSphereNonAlloc(
            groundCheck.position,
            groundDistance,
            surfaceColliders,
            groundMask
        );

        currentSurface = count > 0
            ? FootstepAudioEvent.DetectSurfaceFromTag(surfaceColliders[0].tag)
            : SurfaceType.Concrete;
    }
    #endregion

    #region Jump
    private void UpdateJump()
    {
        if (!enableJump || !jumpPressed)
        {
            jumpPressed = false;
            return;
        }

        if (CanJump())
        {
            moveDir.y = jumpForce;
            PlayJumpSound();
        }

        jumpPressed = false;
    }

    private bool CanJump()
    {
        return (isGrounded || CanCoyoteJump()) && !IsCrouching;
    }

    private void PlayJumpSound()
    {
        if (jumpSound != null && SOAudioManager.Instance != null)
        {
            SOAudioManager.Instance.PlaySFX(jumpSound);
        }
    }

    private void UpdateGravity()
    {
        moveDir.y -= gravity * Time.deltaTime;

        if (isGrounded && moveDir.y < 0f)
            moveDir.y = -2f;
    }
    #endregion

    #region Crouch
    private void UpdateCrouch()
    {
        if (!enableCrouch) return;

        UpdateTargetHeight();
        InterpolateHeight();
        UpdateCameraPosition();
    }

    private void UpdateTargetHeight()
    {
        if (!crouchHeld && !CanStandUp())
        {
            targetHeight = crouchHeight;
        }
        else
        {
            targetHeight = crouchHeld ? crouchHeight : defaultHeight;
        }
    }

    private bool CanStandUp()
    {
        float distanceToCheck = defaultHeight - currentHeight + 0.1f;
        Vector3 start = transform.position + Vector3.up * currentHeight / 2f;

        return !Physics.SphereCast(start, controller.radius, Vector3.up, out _, distanceToCheck);
    }

    private void InterpolateHeight()
    {
        float heightDelta = Mathf.Abs(currentHeight - targetHeight);
        float speed = crouchSpeed * Mathf.Max(0.2f, heightDelta);

        currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, Time.deltaTime * speed);

        controller.height = currentHeight;
        controller.center = new Vector3(0f, currentHeight / 2f, 0f);
    }

    private void UpdateCameraPosition()
    {
        if (playerCamera == null) return;

        float targetCameraY = currentHeight - 0.1f;
        Vector3 pos = playerCamera.localPosition;
        pos.y = Mathf.Lerp(pos.y, targetCameraY, Time.deltaTime * crouchSpeed);
        playerCamera.localPosition = pos;
    }
    #endregion

    #region Debug
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(groundCheck.position, 0.05f);
    }
#endif
    #endregion

    #region Enums
    public enum CrouchMode
    {
        Hold,
        Toggle
    }
    #endregion
}