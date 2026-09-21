using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public CharacterController controller;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;

    private Vector3 lastPosition;
    private bool waitingForRunStop;


    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        lastPosition = transform.position;
    }


    void Update()
    {
        // -------------------------
        // ACTUAL MOVEMENT SPEED
        // -------------------------

        Vector3 movement =
            transform.position - lastPosition;

        // Ignore vertical movement
        movement.y = 0f;

        float currentSpeed =
            movement.magnitude / Time.deltaTime;

        lastPosition = transform.position;


        // -------------------------
        // WALK SPEED
        // -------------------------

        float walkAmount =
            Mathf.Clamp01(currentSpeed / walkSpeed);


        // -------------------------
        // RUN SPEED
        // -------------------------

        float runAmount =
            Mathf.Clamp01(currentSpeed / runSpeed);


        // -------------------------
        // WALKING BACKWARD
        // -------------------------

        bool walkingBackward =
            Input.GetKey(KeyCode.S) &&
            controller.isGrounded;

        animator.SetBool(
            "WalkingBackward",
            walkingBackward
        );


        // -------------------------
        // RUNNING
        // -------------------------

        bool isRunning =
            Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKey(KeyCode.W) &&
            controller.isGrounded;


        // -------------------------
        // WALK / RUN ANIMATION
        // -------------------------

        if (walkingBackward)
        {
            // Stop forward Walk/Run animation parameters
            // because WalkBackward handles the animation

            animator.SetFloat(
                "WalkSpeed",
                0f,
                0.1f,
                Time.deltaTime
            );

            animator.SetFloat(
                "RunSpeed",
                0f,
                0.1f,
                Time.deltaTime
            );
        }
        else if (isRunning)
        {
            animator.SetFloat(
                "WalkSpeed",
                0f,
                0.1f,
                Time.deltaTime
            );

            animator.SetFloat(
                "RunSpeed",
                runAmount,
                0.1f,
                Time.deltaTime
            );
        }
        else
        {
            animator.SetFloat(
                "RunSpeed",
                0f,
                0.1f,
                Time.deltaTime
            );

            animator.SetFloat(
                "WalkSpeed",
                walkAmount,
                0.1f,
                Time.deltaTime
            );
        }


        // -------------------------
        // RUN STOP
        // -------------------------

        bool hasMovementInput =
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
            Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;


        // Remember that we were running
        if (isRunning && hasMovementInput)
        {
            waitingForRunStop = true;
        }


        // Trigger RunStop when movement input is released
        if (waitingForRunStop &&
            !hasMovementInput &&
            controller.isGrounded)
        {
            animator.SetTrigger("RunStop");

            waitingForRunStop = false;
        }


        // Releasing Shift while still moving means:
        // Run -> Walk instead of Run -> RunStop
        if (!Input.GetKey(KeyCode.LeftShift) &&
            hasMovementInput)
        {
            waitingForRunStop = false;
        }


        // Don't RunStop while airborne
        if (!controller.isGrounded)
        {
            waitingForRunStop = false;
        }


        // -------------------------
        // GROUNDED
        // -------------------------

        animator.SetBool(
            "Grounded",
            controller.isGrounded
        );


        // -------------------------
        // AIR DASH
        // -------------------------

        if (Input.GetKeyDown(KeyCode.LeftShift) &&
            !controller.isGrounded)
        {
            animator.SetTrigger("Dash");
        }
    }
}