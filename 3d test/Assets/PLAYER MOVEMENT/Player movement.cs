using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;

    [Header("Lock On Movement")]
    public float lockOnSpeed = 7f;

    [Header("Normal Movement Turning")]
    public float walkTurnSpeed = 720f;
    public float runTurnSpeed = 900f;

    [Header("Walking Turn Animations")]
    public float walkTurnCooldown = 0.5f;

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

    [Header("Lock On")]
    public PlayerLockOn lockOn;

    [Header("Stamina")]
    public PlayerStamina stamina;

    private CharacterController controller;
    private Vector3 velocity;

    private bool isGrounded;
    private bool isRunning;
    private bool sprintLocked;

    public bool combatMovementLocked;

    private float jumpAnimationTimer;
    private float airSpeed;

    // =========================================
    // WALK TURN STATE
    // =========================================

    private float lastWalkTurnTime = -999f;
    private Vector3 previousWalkInput = Vector3.zero;

    // =========================================
    // PUBLIC STATES
    // =========================================

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

    // =========================================
    // START
    // =========================================

    void Start()
    {
        controller =
            GetComponent<CharacterController>();

        if (stamina == null)
        {
            stamina =
                GetComponent<PlayerStamina>();
        }

        if (lockOn == null)
        {
            lockOn =
                GetComponent<PlayerLockOn>();
        }

        airSpeed = walkSpeed;

        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;
        }

        if (controller == null)
        {
            Debug.LogError(
                "CHARACTER CONTROLLER IS NOT ASSIGNED!"
            );
        }

        if (animator == null)
        {
            Debug.LogError(
                "ANIMATOR IS NOT ASSIGNED!"
            );
        }

        if (cameraTransform == null)
        {
            Debug.LogError(
                "CAMERA IS NOT ASSIGNED!"
            );
        }
    }

    // =========================================
    // UPDATE
    // =========================================

    void Update()
    {
        // =====================================
        // GROUND CHECK
        // =====================================

        isGrounded =
            Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundMask
            );

        if (isGrounded &&
            velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // =====================================
        // RAW WASD INPUT
        // =====================================

        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        bool hasMovementInput =
            Mathf.Abs(horizontal) > 0.01f ||
            Mathf.Abs(vertical) > 0.01f;

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

        // =====================================
        // CAMERA RELATIVE MOVEMENT
        // =====================================

        Vector3 moveDirection =
            Vector3.zero;

        if (cameraTransform != null &&
            inputDirection.sqrMagnitude > 0.001f)
        {
            Vector3 cameraForward =
                cameraTransform.forward;

            cameraForward.y = 0f;

            if (cameraForward.sqrMagnitude > 0.001f)
            {
                cameraForward.Normalize();
            }

            Vector3 cameraRight =
                cameraTransform.right;

            cameraRight.y = 0f;

            if (cameraRight.sqrMagnitude > 0.001f)
            {
                cameraRight.Normalize();
            }

            moveDirection =
                cameraForward * inputDirection.z +
                cameraRight * inputDirection.x;

            moveDirection =
                Vector3.ClampMagnitude(
                    moveDirection,
                    1f
                );
        }

        // =====================================
        // SPRINT LOCK
        // =====================================

        if (!isGrounded &&
            Input.GetKey(KeyCode.LeftShift))
        {
            sprintLocked = true;
        }

        if (isGrounded &&
            !Input.GetKey(KeyCode.LeftShift))
        {
            sprintLocked = false;
        }

        // =====================================
        // RUN STATE
        // =====================================

        bool canSprint =
            stamina == null ||
            !stamina.IsExhausted;

        isRunning =
            isGrounded &&
            hasMovementInput &&
            Input.GetKey(KeyCode.LeftShift) &&
            !sprintLocked &&
            canSprint &&
            (lockOn == null ||
             !lockOn.IsLockedOn);

        if (isRunning &&
            stamina != null)
        {
            stamina.DrainSprintStamina();
        }

        // =====================================
        // WALK LEFT TURN DETECTION
        // =====================================

        bool lockOnActive =
            lockOn != null &&
            lockOn.IsLockedOn;

        bool walking =
            isGrounded &&
            hasMovementInput &&
            !isRunning &&
            !lockOnActive;

        bool cooldownReady =
            Time.time >=
            lastWalkTurnTime +
            walkTurnCooldown;

        // Previous frame was mainly FORWARD.
        bool wasWalkingForward =
            previousWalkInput.z > 0.7f &&
            Mathf.Abs(previousWalkInput.x) < 0.5f;

        // Current frame has a strong LEFT input.
        bool suddenlyTurnedLeft =
            horizontal < -0.7f;

        if (walking &&
            wasWalkingForward &&
            suddenlyTurnedLeft &&
            cooldownReady &&
            animator != null)
        {
            animator.ResetTrigger(
                "WalkTurnLeft"
            );

            animator.SetTrigger(
                "WalkTurnLeft"
            );

            lastWalkTurnTime =
                Time.time;
        }

        // Remember raw input for the next frame.
        previousWalkInput =
            inputDirection;

        // =====================================
        // MOVEMENT SPEED
        // =====================================

        float currentSpeed;

        if (isGrounded)
        {
            if (lockOnActive)
            {
                currentSpeed =
                    lockOnSpeed;
            }
            else
            {
                currentSpeed =
                    isRunning
                        ? runSpeed
                        : walkSpeed;
            }

            airSpeed = currentSpeed;
        }
        else
        {
            currentSpeed = airSpeed;
        }

        // =====================================
        // NORMAL MOVEMENT ROTATION
        // =====================================

        if (hasMovementInput &&
            moveDirection.sqrMagnitude > 0.01f &&
            !lockOnActive)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection.normalized,
                    Vector3.up
                );

            float currentTurnSpeed =
                isRunning
                    ? runTurnSpeed
                    : walkTurnSpeed;

            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    currentTurnSpeed *
                    Time.deltaTime
                );
        }

        // =====================================
        // HORIZONTAL MOVEMENT
        // =====================================

        if (hasMovementInput &&
            !combatMovementLocked)
        {
            controller.Move(
                moveDirection *
                currentSpeed *
                Time.deltaTime
            );
        }

        // =====================================
        // JUMP
        // =====================================

        bool canJump =
            lockOn == null ||
            !lockOn.IsLockedOn;

        if (Input.GetKeyDown(KeyCode.Space) &&
            isGrounded &&
            canJump)
        {
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

                animator.SetTrigger(
                    "Jump"
                );
            }
        }

        // =====================================
        // GRAVITY
        // =====================================

        velocity.y +=
            gravity *
            Time.deltaTime;

        controller.Move(
            velocity *
            Time.deltaTime
        );

        // =====================================
        // JUMP / FALL ANIMATION
        // =====================================

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