using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;

    [Header("Rotation Smoothing")]
    public float rotationSmoothTime = 0.12f;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Jump")]
    public float jumpHeight = 2f;
    public float gravity = -20f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Animation")]
    public Animator animator;
    public float jumpAnimationTime = 0.4f;

    private CharacterController controller;

    private Vector3 velocity;

    private bool isGrounded;
    private bool isRunning;
    private bool sprintLocked;

    private float jumpAnimationTimer;
    private float airSpeed;

    // Smooth visual turning only
    private float rotationVelocity;


    // =====================================================
    // PUBLIC STATES
    // =====================================================

    public bool IsGrounded
    {
        get { return isGrounded; }
    }

    public bool IsRunning
    {
        get { return isRunning; }
    }

    public bool HasMovementInput
    {
        get
        {
            return
                Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
                Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
        }
    }


    void Start()
    {
        controller = GetComponent<CharacterController>();

        airSpeed = walkSpeed;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (animator == null)
        {
            Debug.LogError("ANIMATOR IS NOT ASSIGNED!");
        }

        if (cameraTransform == null)
        {
            Debug.LogError("CAMERA IS NOT ASSIGNED!");
        }
    }


    void Update()
    {
        // -------------------------
        // GROUND CHECK
        // -------------------------

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }


        // -------------------------
        // INPUT
        // -------------------------

        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        Vector3 inputDirection =
            new Vector3(
                horizontal,
                0f,
                vertical
            );

        inputDirection =
            Vector3.ClampMagnitude(
                inputDirection,
                1f
            );

        bool hasMovementInput =
            inputDirection.sqrMagnitude > 0.01f;


        // -------------------------
        // CAMERA RELATIVE MOVEMENT
        // -------------------------

        Vector3 moveDirection =
            Vector3.zero;

        if (cameraTransform != null &&
            hasMovementInput)
        {
            Vector3 cameraForward =
                cameraTransform.forward;

            cameraForward.y = 0f;
            cameraForward.Normalize();


            Vector3 cameraRight =
                cameraTransform.right;

            cameraRight.y = 0f;
            cameraRight.Normalize();


            moveDirection =
                cameraForward * inputDirection.z +
                cameraRight * inputDirection.x;

            moveDirection =
                Vector3.ClampMagnitude(
                    moveDirection,
                    1f
                );
        }


        // -------------------------
        // SPRINT LOCK
        // -------------------------

        if (!isGrounded &&
            Input.GetKey(KeyCode.LeftShift))
        {
            sprintLocked = true;
        }

        // Must release Shift after landing
        if (isGrounded &&
            !Input.GetKey(KeyCode.LeftShift))
        {
            sprintLocked = false;
        }


        // -------------------------
        // RUN STATE
        // -------------------------

        isRunning =
            isGrounded &&
            hasMovementInput &&
            Input.GetKey(KeyCode.LeftShift) &&
            !sprintLocked;


        // -------------------------
        // MOVEMENT SPEED
        // -------------------------

        float currentSpeed;

        if (isGrounded)
        {
            currentSpeed =
                isRunning
                    ? runSpeed
                    : walkSpeed;

            // Remember takeoff speed
            airSpeed = currentSpeed;
        }
        else
        {
            // Keep horizontal speed in the air
            currentSpeed = airSpeed;
        }


        // -------------------------
        // SMOOTH PLAYER ROTATION
        // -------------------------

        if (hasMovementInput &&
            moveDirection.sqrMagnitude > 0.01f)
        {
            float targetAngle =
                Mathf.Atan2(
                    moveDirection.x,
                    moveDirection.z
                ) * Mathf.Rad2Deg;

            float smoothAngle =
                Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref rotationVelocity,
                    rotationSmoothTime
                );

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    smoothAngle,
                    0f
                );
        }


        // -------------------------
        // HORIZONTAL MOVEMENT
        // -------------------------

        if (hasMovementInput)
        {
            controller.Move(
                moveDirection *
                currentSpeed *
                Time.deltaTime
            );
        }


        // -------------------------
        // JUMP
        // -------------------------

        if (Input.GetKeyDown(KeyCode.Space) &&
            isGrounded)
        {
            // Preserve current ground speed
            airSpeed =
                isRunning
                    ? runSpeed
                    : walkSpeed;

            velocity.y =
                Mathf.Sqrt(
                    jumpHeight *
                    -2f *
                    gravity
                );

            jumpAnimationTimer =
                jumpAnimationTime;

            if (animator != null)
            {
                animator.SetBool(
                    "Falling",
                    false
                );

                animator.SetTrigger("Jump");
            }
        }


        // -------------------------
        // GRAVITY
        // -------------------------

        velocity.y +=
            gravity *
            Time.deltaTime;

        controller.Move(
            velocity *
            Time.deltaTime
        );


        // -------------------------
        // JUMP / FALL ANIMATION
        // -------------------------

        if (jumpAnimationTimer > 0f)
        {
            jumpAnimationTimer -=
                Time.deltaTime;
        }

        if (animator != null)
        {
            animator.SetBool(
                "Grounded",
                isGrounded
            );

            bool isFalling =
                !isGrounded &&
                velocity.y < 0f &&
                jumpAnimationTimer <= 0f;

            animator.SetBool(
                "Falling",
                isFalling
            );
        }
    }
}