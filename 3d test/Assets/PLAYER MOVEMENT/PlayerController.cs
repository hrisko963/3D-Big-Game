using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public CharacterController controller;
    public PlayerMovement playerMovement;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;

    [Header("Animation Smoothing")]
    [Tooltip("How smoothly movement animations accelerate.")]
    public float animationSmoothTime = 0.05f;

    [Tooltip("How smoothly movement animations return to idle.")]
    public float animationStopSmoothTime = 0.18f;

    [Header("Run Stop")]
    public float runStopMemoryTime = 0.2f;

    [Tooltip(
        "Small delay before RunStop plays. " +
        "Prevents RunStop from firing during fast direction changes."
    )]
    public float runStopInputGraceTime = 0.08f;

    private Vector3 lastPosition;

    private bool wasGrounded;
    private bool hadMovementInput;

    private float lastRunningTime = -999f;

    // RunStop grace period
    private bool waitingForRunStop;
    private float runStopReleaseTime;

    void Start()
    {
        if (controller == null)
            controller =
                GetComponent<CharacterController>();

        if (animator == null)
            animator =
                GetComponent<Animator>();

        if (playerMovement == null)
            playerMovement =
                GetComponent<PlayerMovement>();

        lastPosition =
            transform.position;

        if (playerMovement != null)
        {
            wasGrounded =
                playerMovement.IsGrounded;

            hadMovementInput =
                playerMovement.HasMovementInput;
        }
    }

    void Update()
    {
        if (animator == null ||
            playerMovement == null)
        {
            return;
        }

        // ========================================
        // ACTUAL MOVEMENT SPEED
        // ========================================

        Vector3 movement =
            transform.position -
            lastPosition;

        movement.y = 0f;

        float currentSpeed =
            movement.magnitude /
            Mathf.Max(
                Time.deltaTime,
                0.0001f
            );

        lastPosition =
            transform.position;

        bool isGrounded =
            playerMovement.IsGrounded;

        bool isRunning =
            playerMovement.IsRunning;

        bool hasMovementInput =
            playerMovement.HasMovementInput;

        animator.SetBool(
            "Grounded",
            isGrounded
        );

        // ========================================
        // REMEMBER RECENT RUNNING
        // ========================================

        if (isRunning &&
            hasMovementInput &&
            isGrounded)
        {
            lastRunningTime =
                Time.time;
        }

        // ========================================
        // LOCOMOTION VALUES
        // ========================================

        float walkAmount =
            Mathf.Clamp01(
                currentSpeed /
                walkSpeed
            );

        float runAmount =
            Mathf.Clamp01(
                currentSpeed /
                runSpeed
            );

        // ========================================
        // AIRBORNE
        // ========================================

        if (!isGrounded)
        {
            animator.SetFloat(
                "WalkSpeed",
                0f,
                animationStopSmoothTime,
                Time.deltaTime
            );

            animator.SetFloat(
                "RunSpeed",
                0f,
                animationStopSmoothTime,
                Time.deltaTime
            );
        }

        // ========================================
        // IDLE
        // ========================================

        else if (!hasMovementInput)
        {
            animator.SetFloat(
                "WalkSpeed",
                0f,
                animationStopSmoothTime,
                Time.deltaTime
            );

            animator.SetFloat(
                "RunSpeed",
                0f,
                animationStopSmoothTime,
                Time.deltaTime
            );
        }

        // ========================================
        // RUNNING
        // ========================================

        else if (isRunning)
        {
            animator.SetFloat(
                "WalkSpeed",
                0f,
                animationSmoothTime,
                Time.deltaTime
            );

            animator.SetFloat(
                "RunSpeed",
                Mathf.Max(
                    runAmount,
                    0.1f
                ),
                animationSmoothTime,
                Time.deltaTime
            );
        }

        // ========================================
        // WALKING
        // ========================================

        else
        {
            animator.SetFloat(
                "RunSpeed",
                0f,
                animationSmoothTime,
                Time.deltaTime
            );

            animator.SetFloat(
                "WalkSpeed",
                Mathf.Max(
                    walkAmount,
                    0.1f
                ),
                animationSmoothTime,
                Time.deltaTime
            );
        }

        // ========================================
        // RUN STOP DETECTION
        // ========================================

        bool movementWasReleased =
            hadMovementInput &&
            !hasMovementInput;

        bool wasRecentlyRunning =
            Time.time -
            lastRunningTime <=
            runStopMemoryTime;

        // Instead of immediately playing RunStop,
        // begin a very short grace period.
        if (movementWasReleased &&
            wasRecentlyRunning &&
            isGrounded)
        {
            waitingForRunStop = true;

            runStopReleaseTime =
                Time.time;
        }

        // ========================================
        // CANCEL RUN STOP IF INPUT RETURNS
        // ========================================

        // If WASD comes back during the grace
        // period, the player was most likely
        // changing direction instead of stopping.
        if (waitingForRunStop &&
            hasMovementInput)
        {
            waitingForRunStop = false;
        }

        // ========================================
        // ACTUALLY PLAY RUN STOP
        // ========================================

        if (waitingForRunStop &&
            !hasMovementInput &&
            isGrounded)
        {
            float timeWithoutInput =
                Time.time -
                runStopReleaseTime;

            if (timeWithoutInput >=
                runStopInputGraceTime)
            {
                waitingForRunStop = false;

                // Immediately clear locomotion
                // values before RunStop.
                animator.SetFloat(
                    "WalkSpeed",
                    0f
                );

                animator.SetFloat(
                    "RunSpeed",
                    0f
                );

                animator.ResetTrigger(
                    "RunStop"
                );

                animator.SetTrigger(
                    "RunStop"
                );

                lastRunningTime =
                    -999f;
            }
        }

        // ========================================
        // LANDING
        // ========================================

        bool justLanded =
            !wasGrounded &&
            isGrounded;

        if (justLanded)
        {
            animator.ResetTrigger(
                "RunStop"
            );

            waitingForRunStop =
                false;

            lastRunningTime =
                -999f;
        }

        // ========================================
        // SAVE STATE
        // ========================================

        hadMovementInput =
            hasMovementInput;

        wasGrounded =
            isGrounded;
    }
}