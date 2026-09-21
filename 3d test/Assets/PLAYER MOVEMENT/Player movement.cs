using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float rotationSpeed = 12f;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Jump")]
    public float jumpHeight = 2f;
    public float gravity = -20f;

    [Header("Air Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Animation")]
    public Animator animator;
    public float jumpAnimationTime = 0.4f;

    private CharacterController controller;

    private Vector3 velocity;
    private Vector3 dashDirection;

    private bool isGrounded;
    private bool isRunning;
    private bool isDashing;
    private bool canDash;

    private float dashTimer;
    private float jumpAnimationTimer;


    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Automatically use Main Camera if none is assigned
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

        if (isGrounded)
        {
            if (velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            canDash = true;
            isDashing = false;
        }


        // -------------------------
        // INPUT
        // -------------------------

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection =
            new Vector3(horizontal, 0f, vertical);

        inputDirection = Vector3.ClampMagnitude(
            inputDirection,
            1f
        );


        // -------------------------
        // CAMERA-RELATIVE MOVEMENT
        // -------------------------

        Vector3 moveDirection = Vector3.zero;

        if (cameraTransform != null)
        {
            // Camera forward without vertical tilt
            Vector3 cameraForward =
                cameraTransform.forward;

            cameraForward.y = 0f;
            cameraForward.Normalize();


            // Camera right without vertical tilt
            Vector3 cameraRight =
                cameraTransform.right;

            cameraRight.y = 0f;
            cameraRight.Normalize();


            // Convert WASD into camera-relative direction
            moveDirection =
                cameraForward * inputDirection.z +
                cameraRight * inputDirection.x;

            moveDirection = Vector3.ClampMagnitude(
                moveDirection,
                1f
            );
        }


        // -------------------------
        // RUN
        // -------------------------

        bool hasMovement =
            moveDirection.sqrMagnitude > 0.01f;

        isRunning =
            Input.GetKey(KeyCode.LeftShift) &&
            hasMovement &&
            isGrounded;

        float currentSpeed =
            isRunning ? runSpeed : walkSpeed;


        // -------------------------
        // ROTATE PLAYER
        // -------------------------

        if (hasMovement && !isDashing)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
        }


        // -------------------------
        // MOVE PLAYER
        // -------------------------

        if (!isDashing)
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
            velocity.y =
                Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );

            canDash = true;

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
        // AIR DASH
        // -------------------------

        if (!isGrounded &&
            Input.GetKeyDown(KeyCode.LeftShift) &&
            canDash &&
            !isDashing)
        {
            StartDash(moveDirection);
        }


        if (isDashing)
        {
            controller.Move(
                dashDirection *
                dashSpeed *
                Time.deltaTime
            );

            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }


        // -------------------------
        // GRAVITY
        // -------------------------

        if (!isDashing)
        {
            velocity.y +=
                gravity * Time.deltaTime;

            controller.Move(
                velocity * Time.deltaTime
            );
        }


        // -------------------------
        // ANIMATION
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


    // =====================================================
    // AIR DASH
    // =====================================================

    void StartDash(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
        }

        dashDirection =
            direction.normalized;

        isDashing = true;
        canDash = false;

        dashTimer = dashDuration;

        velocity.y = 0f;
    }
}