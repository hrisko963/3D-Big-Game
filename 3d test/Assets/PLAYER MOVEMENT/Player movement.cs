using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // ==================================================
    // MOVEMENT
    // ==================================================

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;

    public float walkAcceleration = 40f;
    public float walkDeceleration = 55f;

    public float runAcceleration = 32f;

    [Tooltip("Lower = more noticeable sprint settling.")]
    public float runDeceleration = 38f;

    public float directionChangeAcceleration = 50f;


    // ==================================================
    // CROUCH
    // ==================================================

    [Header("Crouch")]

    public KeyCode crouchKey = KeyCode.LeftControl;

    public float crouchSpeed = 2.8f;

    public float crouchAcceleration = 45f;
    public float crouchDeceleration = 55f;

    [Tooltip("CharacterController height while crouched.")]
    public float crouchHeight = 1.2f;

    [Tooltip("How quickly the controller changes height.")]
    public float crouchTransitionSpeed = 10f;

    [Tooltip("Layers checked when trying to stand up.")]
    public LayerMask ceilingMask;


    // ==================================================
    // SLIDE
    // ==================================================

    [Header("Slide")]

    [Tooltip("Initial slide speed.")]
    public float slideStartSpeed = 11f;

    [Tooltip("How quickly the slide loses speed.")]
    public float slideDeceleration = 12f;

    [Tooltip("Maximum slide duration.")]
    public float slideDuration = 0.75f;

    [Tooltip("Slide ends when speed drops below this.")]
    public float slideEndSpeed = 3f;


    // ==================================================
    // LOCK ON
    // ==================================================

    [Header("Lock On Movement")]
    public float lockOnSpeed = 7f;

    public float lockOnAcceleration = 55f;
    public float lockOnDeceleration = 65f;


    // ==================================================
    // TURNING
    // ==================================================

    [Header("Normal Movement Turning")]
    public float walkTurnSpeed = 650f;
    public float runTurnSpeed = 800f;


    // ==================================================
    // CAMERA
    // ==================================================

    [Header("Camera")]
    public Transform cameraTransform;


    // ==================================================
    // JUMP
    // ==================================================

    [Header("Jump")]
    public float jumpHeight = 2f;
    public float gravity = -20f;

    [Header("Air Control")]

    [Range(0f, 1f)]
    public float airControl = 0.45f;

    public float airAcceleration = 8f;


    // ==================================================
    // GROUND CHECK
    // ==================================================

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;


    // ==================================================
    // SLOPE GROUNDING
    // ==================================================

    [Header("Slope Grounding")]
    public float slopeCheckDistance = 0.8f;
    public float groundStickForce = 5f;
    public float jumpGroundIgnoreTime = 0.15f;


    // ==================================================
    // FALLING
    // ==================================================

    [Header("Falling")]
    public float fallAnimationDelay = 0.65f;


    // ==================================================
    // ANIMATION
    // ==================================================

    [Header("Animation")]
    public Animator animator;
    public float jumpAnimationTime = 0.4f;


    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("Lock On")]
    public PlayerLockOn lockOn;

    [Header("Stamina")]
    public PlayerStamina stamina;


    // ==================================================
    // PUBLIC STATE
    // ==================================================

    public bool combatMovementLocked;


    // ==================================================
    // PRIVATE
    // ==================================================

    private CharacterController controller;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    private bool isGrounded;
    private bool isRunning;

    private bool isCrouching;
    private bool isSliding;

    private bool sprintLocked;
    private bool wasRunning;

    private float jumpAnimationTimer;
    private float jumpGroundIgnoreTimer;
    private float airborneFallTimer;

    private float airSpeed;

    private bool hasGroundHit;
    private RaycastHit groundHit;

    private float standingHeight;
    private Vector3 standingCenter;

    private float slideTimer;
    private Vector3 slideDirection;


    // ==================================================
    // PUBLIC PROPERTIES
    // ==================================================

    public bool IsGrounded
    {
        get { return isGrounded; }
    }

    public bool IsRunning
    {
        get { return isRunning; }
    }

    public bool IsCrouching
    {
        get { return isCrouching; }
    }

    public bool IsSliding
    {
        get { return isSliding; }
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


    // ==================================================
    // START
    // ==================================================

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (stamina == null)
            stamina = GetComponent<PlayerStamina>();

        if (lockOn == null)
            lockOn = GetComponent<PlayerLockOn>();

        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (controller != null)
        {
            standingHeight = controller.height;
            standingCenter = controller.center;
        }

        airSpeed = walkSpeed;

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

        if (groundCheck == null)
        {
            Debug.LogError(
                "GROUND CHECK IS NOT ASSIGNED!"
            );
        }
    }


    // ==================================================
    // UPDATE
    // ==================================================

    void Update()
    {
        if (controller == null)
            return;


        // ==================================================
        // TIMERS
        // ==================================================

        if (jumpGroundIgnoreTimer > 0f)
        {
            jumpGroundIgnoreTimer -= Time.deltaTime;
        }

        if (jumpAnimationTimer > 0f)
        {
            jumpAnimationTimer -= Time.deltaTime;
        }


        // ==================================================
        // GROUND CHECK
        // ==================================================

        UpdateGroundCheck();


        // ==================================================
        // INPUT
        // ==================================================

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
            inputDirection.sqrMagnitude > 0.001f;


        // ==================================================
        // CAMERA RELATIVE MOVEMENT
        // ==================================================

        Vector3 moveDirection =
            GetCameraRelativeDirection(
                inputDirection
            );


        // ==================================================
        // LOCK ON
        // ==================================================

        bool lockOnActive =
            lockOn != null &&
            lockOn.IsLockedOn;


        // ==================================================
        // SPRINT LOCK
        // ==================================================

        bool shiftHeld =
            Input.GetKey(KeyCode.LeftShift);

        if (!shiftHeld)
        {
            sprintLocked = false;
        }


        // ==================================================
        // CALCULATE RUN STATE FIRST
        // ==================================================
        //
        // IMPORTANT:
        // We calculate sprint BEFORE checking Ctrl.
        //
        // This means:
        //
        // W + Shift = Sprint
        //
        // Then pressing Ctrl while still holding Shift
        // can start the slide.
        //
        // Walking + Ctrl cannot start a slide.
        // ==================================================

        bool canSprint =
            stamina == null ||
            !stamina.IsExhausted;

        isRunning =
            isGrounded &&
            hasMovementInput &&
            shiftHeld &&
            !sprintLocked &&
            canSprint &&
            !lockOnActive &&
            !isCrouching &&
            !isSliding;


        // ==================================================
        // CROUCH / SLIDE INPUT
        // ==================================================

        HandleCrouchAndSlideInput();


        // ==================================================
        // STAMINA
        // ==================================================

        if (isRunning &&
            !isSliding &&
            stamina != null)
        {
            stamina.DrainSprintStamina();
        }


        // ==================================================
        // JUMP
        // ==================================================

        bool canJump =
            (lockOn == null ||
             !lockOn.IsLockedOn) &&
            !isCrouching &&
            !isSliding;

        if (Input.GetKeyDown(KeyCode.Space) &&
            isGrounded &&
            canJump)
        {
            Jump();
        }


        // ==================================================
        // SLOPE DIRECTION
        // ==================================================

        if (isGrounded &&
            hasGroundHit &&
            hasMovementInput &&
            !isSliding &&
            jumpGroundIgnoreTimer <= 0f)
        {
            Vector3 slopeDirection =
                Vector3.ProjectOnPlane(
                    moveDirection,
                    groundHit.normal
                );

            if (slopeDirection.sqrMagnitude > 0.001f)
            {
                moveDirection =
                    slopeDirection.normalized;
            }
        }


        // ==================================================
        // HORIZONTAL MOVEMENT
        // ==================================================

        if (isSliding)
        {
            UpdateSlide();
        }
        else if (!combatMovementLocked)
        {
            if (isGrounded)
            {
                UpdateGroundMovement(
                    moveDirection,
                    hasMovementInput,
                    lockOnActive
                );
            }
            else
            {
                UpdateAirMovement(
                    moveDirection,
                    hasMovementInput
                );
            }
        }
        else
        {
            horizontalVelocity =
                Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    walkDeceleration *
                    Time.deltaTime
                );
        }


        // ==================================================
        // ROTATION
        // ==================================================

        if (!isSliding &&
            hasMovementInput &&
            moveDirection.sqrMagnitude > 0.001f &&
            !lockOnActive &&
            !combatMovementLocked)
        {
            RotateCharacter(
                moveDirection
            );
        }


        // ==================================================
        // GRAVITY
        // ==================================================

        if (!isGrounded ||
            jumpGroundIgnoreTimer > 0f)
        {
            verticalVelocity +=
                gravity *
                Time.deltaTime;
        }
        else if (verticalVelocity <= 0f)
        {
            verticalVelocity =
                -groundStickForce;
        }


        // ==================================================
        // CROUCH CONTROLLER HEIGHT
        // ==================================================

        UpdateControllerHeight();


        // ==================================================
        // ONE CHARACTER CONTROLLER MOVE
        // ==================================================

        Vector3 finalVelocity =
            horizontalVelocity +
            Vector3.up *
            verticalVelocity;

        controller.Move(
            finalVelocity *
            Time.deltaTime
        );


        // ==================================================
        // FALL TIMER
        // ==================================================

        UpdateFallTimer();


        // ==================================================
        // ANIMATION
        // ==================================================

        UpdateAnimation();


        // ==================================================
        // REMEMBER RUN STATE
        // ==================================================

        wasRunning = isRunning;
    }


    // ==================================================
    // CROUCH / SLIDE INPUT
    // ==================================================

    void HandleCrouchAndSlideInput()
    {
        bool shiftHeld =
            Input.GetKey(KeyCode.LeftShift);


        // ==================================================
        // CTRL PRESSED
        // ==================================================

        if (Input.GetKeyDown(crouchKey))
        {
            // ==============================================
            // SLIDE
            // ==============================================
            //
            // A slide can ONLY begin when:
            //
            // - Player is grounded
            // - Player is already sprinting
            // - Left Shift is STILL held
            // - Ctrl was just pressed
            //
            // Walking + Ctrl = crouch.
            // ==============================================

            if (!isSliding &&
                isGrounded &&
                isRunning &&
                shiftHeld)
            {
                StartSlide();
                return;
            }


            // ==============================================
            // NORMAL CROUCH
            // ==============================================

            if (!isSliding)
            {
                isCrouching = true;

                // Once we crouch normally,
                // we are no longer running.
                isRunning = false;
            }
        }


        // ==================================================
        // KEEP CROUCHING WHILE CTRL IS HELD
        // ==================================================

        if (Input.GetKey(crouchKey) &&
            !isSliding)
        {
            isCrouching = true;
            isRunning = false;
        }


        // ==================================================
        // CTRL RELEASED
        // ==================================================

        if (Input.GetKeyUp(crouchKey) &&
            !isSliding)
        {
            TryStandUp();
        }
    }


    // ==================================================
    // START SLIDE
    // ==================================================

    void StartSlide()
    {
        if (!isGrounded)
            return;

        // Safety:
        // Slide still requires Shift.
        if (!Input.GetKey(KeyCode.LeftShift))
            return;

        isSliding = true;
        isCrouching = false;

        // We're transitioning FROM running INTO sliding.
        isRunning = false;

        slideTimer = slideDuration;


        // ==================================================
        // SLIDE DIRECTION
        // ==================================================

        Vector3 flatVelocity =
            horizontalVelocity;

        flatVelocity.y = 0f;

        if (flatVelocity.sqrMagnitude > 0.01f)
        {
            slideDirection =
                flatVelocity.normalized;
        }
        else
        {
            slideDirection =
                transform.forward;
        }


        // ==================================================
        // SLIDE SPEED
        // ==================================================

        float startingSpeed =
            Mathf.Max(
                slideStartSpeed,
                flatVelocity.magnitude
            );

        horizontalVelocity =
            slideDirection *
            startingSpeed;


        // ==================================================
        // ANIMATION
        // ==================================================

        if (animator != null)
        {
            animator.SetBool(
                "Crouching",
                false
            );

            animator.SetBool(
                "Sliding",
                true
            );
        }
    }


    // ==================================================
    // UPDATE SLIDE
    // ==================================================

    void UpdateSlide()
    {
        slideTimer -= Time.deltaTime;


        // ==================================================
        // LOSE SPEED
        // ==================================================

        float currentSpeed =
            horizontalVelocity.magnitude;

        currentSpeed =
            Mathf.MoveTowards(
                currentSpeed,
                0f,
                slideDeceleration *
                Time.deltaTime
            );


        // ==================================================
        // FOLLOW SLOPES
        // ==================================================

        Vector3 direction =
            slideDirection;

        if (hasGroundHit)
        {
            Vector3 slopeDirection =
                Vector3.ProjectOnPlane(
                    direction,
                    groundHit.normal
                );

            if (slopeDirection.sqrMagnitude > 0.001f)
            {
                direction =
                    slopeDirection.normalized;
            }
        }

        slideDirection = direction;

        horizontalVelocity =
            slideDirection *
            currentSpeed;


        // ==================================================
        // END SLIDE
        // ==================================================

        if (slideTimer <= 0f ||
            !isGrounded)
        {
            EndSlide();
        }
    }


    // ==================================================
    // END SLIDE
    // ==================================================

    void EndSlide()
    {
        if (!isSliding)
            return;

        isSliding = false;


        // ==================================================
        // CTRL STILL HELD?
        // ==================================================
        //
        // If Ctrl is still held when the slide ends,
        // remain crouched.
        //
        // If Ctrl is released, stand if there is room.
        // ==================================================

        if (Input.GetKey(crouchKey))
        {
            isCrouching = true;
        }
        else
        {
            if (CanStandUp())
            {
                isCrouching = false;
            }
            else
            {
                isCrouching = true;
            }
        }


        if (animator != null)
        {
            animator.SetBool(
                "Sliding",
                false
            );

            animator.SetBool(
                "Crouching",
                isCrouching
            );
        }
    }


    // ==================================================
    // TRY TO STAND
    // ==================================================

    void TryStandUp()
    {
        if (CanStandUp())
        {
            isCrouching = false;
        }
        else
        {
            // Ceiling above us.
            // Stay crouched.
            isCrouching = true;
        }
    }


    // ==================================================
    // CAN STAND?
    // ==================================================

    bool CanStandUp()
    {
        if (controller == null)
            return true;


        // If no ceiling mask is assigned,
        // allow standing.
        if (ceilingMask.value == 0)
            return true;


        float radius =
            Mathf.Max(
                0.05f,
                controller.radius * 0.9f
            );

        Vector3 worldCenter =
            transform.TransformPoint(
                standingCenter
            );

        float halfHeight =
            Mathf.Max(
                standingHeight * 0.5f,
                radius
            );

        Vector3 bottom =
            worldCenter +
            Vector3.down *
            (halfHeight - radius);

        Vector3 top =
            worldCenter +
            Vector3.up *
            (halfHeight - radius);


        bool blocked =
            Physics.CheckCapsule(
                bottom,
                top,
                radius,
                ceilingMask,
                QueryTriggerInteraction.Ignore
            );

        return !blocked;
    }


    // ==================================================
    // CONTROLLER HEIGHT
    // ==================================================

    void UpdateControllerHeight()
    {
        bool shortController =
            isCrouching ||
            isSliding;


        float targetHeight =
            shortController
                ? crouchHeight
                : standingHeight;


        float newHeight =
            Mathf.MoveTowards(
                controller.height,
                targetHeight,
                crouchTransitionSpeed *
                Time.deltaTime
            );


        // Keep the bottom of the CharacterController
        // approximately in the same place while crouching.
        float bottomPosition =
            standingCenter.y -
            standingHeight * 0.5f;


        Vector3 targetCenter =
            standingCenter;

        targetCenter.y =
            bottomPosition +
            newHeight * 0.5f;


        controller.height =
            newHeight;


        controller.center =
            Vector3.MoveTowards(
                controller.center,
                targetCenter,
                crouchTransitionSpeed *
                Time.deltaTime
            );
    }


    // ==================================================
    // GROUND MOVEMENT
    // ==================================================

    void UpdateGroundMovement(
        Vector3 moveDirection,
        bool hasMovementInput,
        bool lockOnActive)
    {
        // ==================================================
        // NO MOVEMENT INPUT
        // ==================================================

        if (!hasMovementInput)
        {
            float deceleration;

            if (isCrouching)
            {
                deceleration =
                    crouchDeceleration;
            }
            else if (wasRunning)
            {
                // This creates the small physical
                // sprint-settle feeling.
                deceleration =
                    runDeceleration;
            }
            else if (lockOnActive)
            {
                deceleration =
                    lockOnDeceleration;
            }
            else
            {
                deceleration =
                    walkDeceleration;
            }


            horizontalVelocity =
                Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    deceleration *
                    Time.deltaTime
                );

            return;
        }


        // ==================================================
        // TARGET SPEED / ACCELERATION
        // ==================================================

        float targetSpeed;
        float acceleration;


        // CROUCH
        if (isCrouching)
        {
            targetSpeed =
                crouchSpeed;

            acceleration =
                crouchAcceleration;
        }

        // LOCK ON
        else if (lockOnActive)
        {
            targetSpeed =
                lockOnSpeed;

            acceleration =
                lockOnAcceleration;
        }

        // SPRINT
        else if (isRunning)
        {
            targetSpeed =
                runSpeed;

            acceleration =
                runAcceleration;
        }

        // WALK
        else
        {
            targetSpeed =
                walkSpeed;

            acceleration =
                walkAcceleration;
        }


        Vector3 targetVelocity =
            moveDirection *
            targetSpeed;


        // ==================================================
        // DIRECTION CHANGE
        // ==================================================

        Vector3 currentFlatVelocity =
            horizontalVelocity;

        currentFlatVelocity.y = 0f;


        if (currentFlatVelocity.sqrMagnitude > 0.01f &&
            targetVelocity.sqrMagnitude > 0.01f)
        {
            float directionDot =
                Vector3.Dot(
                    currentFlatVelocity.normalized,
                    targetVelocity.normalized
                );


            // Changing direction should be responsive,
            // even though acceleration has some weight.
            if (directionDot < 0.25f)
            {
                acceleration =
                    Mathf.Max(
                        acceleration,
                        directionChangeAcceleration
                    );
            }
        }


        // ==================================================
        // APPLY HORIZONTAL ACCELERATION
        // ==================================================

        horizontalVelocity =
            Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                acceleration *
                Time.deltaTime
            );


        airSpeed =
            horizontalVelocity.magnitude;
    }


    // ==================================================
    // AIR MOVEMENT
    // ==================================================

    void UpdateAirMovement(
        Vector3 moveDirection,
        bool hasMovementInput)
    {
        // No input means preserve takeoff momentum.
        if (!hasMovementInput)
            return;


        float preservedSpeed =
            Mathf.Max(
                airSpeed,
                horizontalVelocity.magnitude
            );


        Vector3 desiredVelocity =
            moveDirection *
            preservedSpeed;


        float controlAcceleration =
            airAcceleration *
            airControl;


        horizontalVelocity =
            Vector3.MoveTowards(
                horizontalVelocity,
                desiredVelocity,
                controlAcceleration *
                Time.deltaTime
            );
    }


    // ==================================================
    // CAMERA RELATIVE MOVEMENT
    // ==================================================

    Vector3 GetCameraRelativeDirection(
        Vector3 inputDirection)
    {
        if (cameraTransform == null ||
            inputDirection.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }


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


        Vector3 direction =
            cameraForward *
            inputDirection.z +
            cameraRight *
            inputDirection.x;


        return Vector3.ClampMagnitude(
            direction,
            1f
        );
    }


    // ==================================================
    // ROTATION
    // ==================================================

    void RotateCharacter(
        Vector3 moveDirection)
    {
        Vector3 flatDirection =
            new Vector3(
                moveDirection.x,
                0f,
                moveDirection.z
            );


        if (flatDirection.sqrMagnitude <= 0.001f)
            return;


        Quaternion targetRotation =
            Quaternion.LookRotation(
                flatDirection.normalized,
                Vector3.up
            );


        float turnSpeed =
            isRunning
                ? runTurnSpeed
                : walkTurnSpeed;


        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed *
                Time.deltaTime
            );
    }


    // ==================================================
    // JUMP
    // ==================================================

    void Jump()
    {
        airSpeed =
            Mathf.Max(
                horizontalVelocity.magnitude,
                isRunning
                    ? runSpeed
                    : walkSpeed
            );


        jumpGroundIgnoreTimer =
            jumpGroundIgnoreTime;


        isGrounded = false;
        hasGroundHit = false;


        verticalVelocity =
            Mathf.Sqrt(
                jumpHeight *
                -2f *
                gravity
            );


        jumpAnimationTimer =
            jumpAnimationTime;


        airborneFallTimer = 0f;


        if (animator != null)
        {
            animator.SetBool(
                "Grounded",
                false
            );

            animator.SetBool(
                "Falling",
                false
            );

            animator.ResetTrigger(
                "Jump"
            );

            animator.SetTrigger(
                "Jump"
            );
        }
    }


    // ==================================================
    // GROUND CHECK
    // ==================================================

    void UpdateGroundCheck()
    {
        bool sphereGrounded = false;


        if (groundCheck != null)
        {
            sphereGrounded =
                Physics.CheckSphere(
                    groundCheck.position,
                    groundDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore
                );
        }


        hasGroundHit = false;


        // Don't snap back onto the floor
        // immediately after jumping.
        if (groundCheck != null &&
            jumpGroundIgnoreTimer <= 0f &&
            verticalVelocity <= 0f)
        {
            Vector3 castOrigin =
                groundCheck.position +
                Vector3.up * 0.3f;


            float castRadius =
                Mathf.Max(
                    0.05f,
                    controller.radius * 0.75f
                );


            hasGroundHit =
                Physics.SphereCast(
                    castOrigin,
                    castRadius,
                    Vector3.down,
                    out groundHit,
                    slopeCheckDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore
                );
        }


        isGrounded =
            jumpGroundIgnoreTimer <= 0f &&
            (
                sphereGrounded ||
                hasGroundHit
            );


        if (isGrounded &&
            verticalVelocity <= 0f)
        {
            verticalVelocity =
                -groundStickForce;
        }
    }


    // ==================================================
    // FALL TIMER
    // ==================================================

    void UpdateFallTimer()
{
    // On the ground = no falling.
    if (isGrounded)
    {
        airborneFallTimer = 0f;
        return;
    }

    // Only start counting once the player
    // is actually moving downward.
    if (verticalVelocity < -0.1f)
    {
        airborneFallTimer += Time.deltaTime;
    }
}


    // ==================================================
    // ANIMATION
    // ==================================================

    void UpdateAnimation()
    {
        if (animator == null)
            return;


        // ==================================================
        // GROUND / FALL
        // ==================================================

        animator.SetBool(
            "Grounded",
            isGrounded
        );


        bool falling =
    !isGrounded &&
    verticalVelocity < -0.1f &&
    airborneFallTimer >= fallAnimationDelay;


        animator.SetBool(
            "Falling",
            falling
        );


        // ==================================================
        // CROUCH / SLIDE
        // ==================================================

        animator.SetBool(
            "Crouching",
            isCrouching
        );


        animator.SetBool(
            "Sliding",
            isSliding
        );


        // ==================================================
        // CROUCH BLEND TREE
        // ==================================================
        //
        // CrouchSpeed = 0 -> Crouch Idle
        // CrouchSpeed = 1 -> Crouch Walk
        // ==================================================

        float targetCrouchSpeed = 0f;


        if (isCrouching)
        {
            float movementInput =
                new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                ).magnitude;


            targetCrouchSpeed =
                Mathf.Clamp01(
                    movementInput
                );
        }


        animator.SetFloat(
            "CrouchSpeed",
            targetCrouchSpeed,
            0.1f,
            Time.deltaTime
        );
    }


    // ==================================================
    // PUBLIC VELOCITY HELPERS
    // ==================================================

    public Vector3 GetHorizontalVelocity()
    {
        return horizontalVelocity;
    }


    public float GetHorizontalSpeed()
    {
        return horizontalVelocity.magnitude;
    }
}