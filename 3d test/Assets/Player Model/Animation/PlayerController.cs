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

    private Vector3 lastPosition;

    private bool waitingForRunStop;
    private bool wasGrounded;


    void Start()
    {
        if (controller == null)
        {
            controller =
                GetComponent<CharacterController>();
        }

        if (animator == null)
        {
            animator =
                GetComponent<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        lastPosition =
            transform.position;

        wasGrounded =
            playerMovement.IsGrounded;
    }


    void Update()
    {
        // -------------------------
        // ACTUAL HORIZONTAL SPEED
        // -------------------------

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


        // -------------------------
        // GET MOVEMENT STATE
        // -------------------------

        bool isGrounded =
            playerMovement.IsGrounded;

        bool isRunning =
            playerMovement.IsRunning;

        bool hasMovementInput =
            playerMovement.HasMovementInput;


        // -------------------------
        // GROUNDED
        // -------------------------

        animator.SetBool(
            "Grounded",
            isGrounded
        );


        // -------------------------
        // ANIMATION SPEED VALUES
        // -------------------------

        float walkAmount =
            Mathf.Clamp01(
                currentSpeed / walkSpeed
            );

        float runAmount =
            Mathf.Clamp01(
                currentSpeed / runSpeed
            );


        // -------------------------
        // AIRBORNE
        // -------------------------

        if (!isGrounded)
        {
            // Jump/Fall animations take control.
            animator.SetFloat(
                "WalkSpeed",
                0f,
                0.08f,
                Time.deltaTime
            );

            animator.SetFloat(
                "RunSpeed",
                0f,
                0.08f,
                Time.deltaTime
            );

            waitingForRunStop = false;
        }


        // -------------------------
        // IDLE
        // -------------------------

        else if (!hasMovementInput)
        {
            animator.SetFloat(
                "WalkSpeed",
                0f,
                0.08f,
                Time.deltaTime
            );

            animator.SetFloat(
                "RunSpeed",
                0f,
                0.08f,
                Time.deltaTime
            );
        }


        // -------------------------
        // RUN
        // -------------------------

        else if (isRunning)
        {
            animator.SetFloat(
                "WalkSpeed",
                0f,
                0.08f,
                Time.deltaTime
            );

            animator.SetFloat(
                "RunSpeed",
                Mathf.Max(
                    runAmount,
                    0.1f
                ),
                0.08f,
                Time.deltaTime
            );
        }


        // -------------------------
        // WALK
        // -------------------------

        else
        {
            animator.SetFloat(
                "RunSpeed",
                0f,
                0.08f,
                Time.deltaTime
            );

            animator.SetFloat(
                "WalkSpeed",
                Mathf.Max(
                    walkAmount,
                    0.1f
                ),
                0.08f,
                Time.deltaTime
            );
        }


        // -------------------------
        // RUN STOP
        // -------------------------

        if (isRunning &&
            hasMovementInput)
        {
            waitingForRunStop = true;
        }


        // Player stopped after running
        if (waitingForRunStop &&
            !hasMovementInput &&
            isGrounded)
        {
            animator.SetTrigger(
                "RunStop"
            );

            waitingForRunStop = false;
        }


        // If player is moving but is no longer
        // actually running, cancel RunStop.
        if (!isRunning &&
            hasMovementInput)
        {
            waitingForRunStop = false;
        }


        // Never RunStop in the air
        if (!isGrounded)
        {
            waitingForRunStop = false;
        }


        // -------------------------
        // LANDING
        // -------------------------

        bool justLanded =
            !wasGrounded &&
            isGrounded;

        if (justLanded)
        {
            animator.ResetTrigger(
                "RunStop"
            );

            waitingForRunStop = false;
        }

        wasGrounded =
            isGrounded;
    }
}