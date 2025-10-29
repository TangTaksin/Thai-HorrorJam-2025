using UnityEngine;
using DG.Tweening;

public class FPHeadBob : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPPlayerController player;

    [Header("Head Bob Settings")]
    [SerializeField, Range(0.01f, 0.1f)] private float bobAmount = 0.05f;
    [SerializeField, Range(1f, 20f)] private float bobFrequency = 12f;
    [SerializeField] private bool enableBob = true; // <-- Enable/disable bob

    [Header("Breathing Settings")]
    [SerializeField, Range(0.001f, 0.05f)] private float breathingAmount = 0.02f;
    [SerializeField, Range(0.5f, 3f)] private float breathingFrequency = 1.5f;
    [SerializeField] private bool enableBreathing = true; // <-- Enable/disable breathing

    [Header("Crouch Multipliers")]
    [SerializeField, Range(0f, 1f)] private float crouchBobMultiplier = 0.5f;
    [SerializeField, Range(0f, 1f)] private float crouchBreathMultiplier = 0.7f;

    [Header("Smoothing")]
    [SerializeField, Range(0.01f, 0.5f)] private float smoothTime = 0.1f;

    private Vector3 initialLocalPos;
    private float bobTimer;
    private float breatheTimer;

    private void Start()
    {
        if (player == null)
            player = GetComponentInParent<FPPlayerController>();

        initialLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (player == null || player.Controller == null) return;

        HandleHeadBobAndBreathing();

    }

    private void HandleHeadBobAndBreathing()
    {
        Vector3 targetPos = initialLocalPos;

        float bobMult = player.IsCrouching ? crouchBobMultiplier : 1f;
        float breathMult = player.IsCrouching ? crouchBreathMultiplier : 1f;

        Vector3 horizontalVel = player.Controller.velocity;
        horizontalVel.y = 0f;
        bool isMoving = horizontalVel.sqrMagnitude > 0.01f && player.Controller.isGrounded;

        // Head bob
        if (enableBob && isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount * bobMult;
            targetPos.y += bobOffset;
        }

        // Breathing
        if (enableBreathing && !isMoving)
        {
            breatheTimer += Time.deltaTime * breathingFrequency;
            float breatheOffset = Mathf.Sin(breatheTimer) * breathingAmount * breathMult;
            targetPos.y += breatheOffset;
        }

        // Adjust for crouch
        if (player.IsCrouching)
        {
            targetPos.y -= (1f - player.Controller.height / 2f);
        }

        

        // Smoothly move camera using DOTween
        transform.DOLocalMove(targetPos, smoothTime)
                 .SetUpdate(true)
                 .SetEase(Ease.OutSine);
    }
}
