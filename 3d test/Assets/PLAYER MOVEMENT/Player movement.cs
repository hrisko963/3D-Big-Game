
using UnityEngine;
using System.Collections;


public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 9f;

    public float turnSpeed = 180f;

    [Header("Jump")]
    public float jumpHeight = 2f;
    public float gravity = -20f;
    public int maxJumps = 2;
    
    [Header("Air Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    
    [Header("Ledge Grab Cooldown")]
    public float ledgeGrabCooldown = 0.5f;

    private float ledgeCooldownTimer = 0f;
    
    [Header("Ledge Grab")]
    public float ledgeCheckDistance = 1.2f;
    public float ledgeCheckHeight = 1.2f;
    public float maxLedgeHeight = 2f;

    public float hangForwardOffset = 0.35f;
    public float hangHeight = 1.0f;

    public float climbHeight = 1.2f;
    public float climbForwardOffset = 0.4f;
    public float climbDuration = 0.8f;

    public LayerMask ledgeMask;
    
    [Header("Drop Hang")]
public float dropHangCheckDistance = 0.8f;
public float dropHangDownDistance = 2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    public Animator animator;
    private CharacterController controller;
    private Vector3 velocity;

    private bool isGrounded;
    private bool isRunning;
    private bool isDashing;
    private bool isHanging;
    private bool isClimbing;


    private int jumpCount;

    private bool canDash;
    private float dashTimer;
    private Vector3 dashDirection;

    [Header("Animation")]
public float jumpAnimationTime = 0.4f;

private float jumpAnimationTimer;

    private Vector3 ledgeHangPosition;
    private Vector3 ledgeClimbPosition;

    void Start()
{
    controller = GetComponent<CharacterController>();

    if (animator == null)
    {
        Debug.LogError("ANIMATOR IS NOT ASSIGNED!");
    }
    else
    {
        Debug.Log("Animator connected to: " + animator.gameObject.name);
    }
}

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
{
    Debug.Log("CTRL pressed - DropHang triggered!");

    if (animator != null)
    {
        animator.SetTrigger("DropHang");
    }
}
        if (ledgeCooldownTimer > 0)
{
    ledgeCooldownTimer -= Time.deltaTime;
}
        // Don't process normal movement while climbing
        if (isClimbing)
            return;

        // -------------------------
        // HANGING
        // -------------------------

        if (isHanging)
        {
            // Stay completely still
            controller.enabled = false;
            transform.position = ledgeHangPosition;
            controller.enabled = true;

            // Press Space to climb
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(ClimbUp());
            }

            // Press Left Ctrl to let go
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                LetGo();
            }

            return;
        }

        // -------------------------
        // GROUND CHECK
        // -------------------------

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );
        if (!isGrounded)
{
    Debug.Log("PLAYER IS IN AIR");
}
        if (isGrounded)
        {
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }

            jumpCount = 0;
            canDash = true;
            isDashing = false;

            // Running only changes while grounded
            isRunning = Input.GetKey(KeyCode.LeftShift);
        }

        // -------------------------
        // MOVEMENT
        // -------------------------

        float x = Input.GetAxis("Horizontal");
float z = Input.GetAxis("Vertical");

// A / D rotates the character
transform.Rotate(
    Vector3.up * x * turnSpeed * Time.deltaTime
);

// W / S moves forward and backward
Vector3 move = transform.forward * z;

        if (!isDashing)
        {
            float currentSpeed =
                isRunning ? runSpeed : walkSpeed;

            controller.Move(
                move * currentSpeed * Time.deltaTime
            );
        }

        // -------------------------
        // JUMP
        // -------------------------

   if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumps)
{
    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

    jumpCount++;
    canDash = true;

    // Let the jump animation play before Fall can start
    jumpAnimationTimer = jumpAnimationTime;

    if (animator != null)
    {
        animator.SetBool("Falling", false);
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
            StartDash(move);
        }

        if (isDashing)
        {
            controller.Move(
                dashDirection *
                dashSpeed *
                Time.deltaTime
            );

            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0)
            {
                isDashing = false;
            }
        }

        // -------------------------
        // GRAVITY
        // -------------------------

if (!isDashing)
{
    velocity.y += gravity * Time.deltaTime;
    controller.Move(velocity * Time.deltaTime);
}

// Falling animation
if (jumpAnimationTimer > 0f)
{
    jumpAnimationTimer -= Time.deltaTime;
}

if (animator != null)
{
    animator.SetBool("Grounded", isGrounded);

    bool isFalling =
    !isGrounded &&
    velocity.y < 0f &&
    jumpAnimationTimer <= 0f;

animator.SetBool("Falling", isFalling);

    animator.SetBool("Falling", isFalling);
}
        // -------------------------
        // LEDGE DETECTION
        // -------------------------

        if (!isGrounded &&
        !isDashing &&
        velocity.y < 0 &&
        ledgeCooldownTimer <= 0)
        {
            CheckForLedge();
        }
    }

    // =====================================================
    // DASH
    // =====================================================

    void StartDash(Vector3 direction)
    {
        if (direction.magnitude < 0.1f)
        {
            direction = transform.forward;
        }

        dashDirection = direction.normalized;

        isDashing = true;
        canDash = false;

        dashTimer = dashDuration;

        velocity.y = 0;
    }

    // =====================================================
    // LEDGE DETECTION
    // =====================================================

    void CheckForLedge()
    {
        Vector3 chestPosition =
            transform.position +
            Vector3.up * ledgeCheckHeight;

        RaycastHit wallHit;

        // Find a wall in front of the player
        if (Physics.Raycast(
            chestPosition,
            transform.forward,
            out wallHit,
            ledgeCheckDistance,
            ledgeMask))
        {
            // Find the top of the platform
            Vector3 topStart =
                wallHit.point +
                transform.forward * 0.15f +
                Vector3.up * maxLedgeHeight;

            RaycastHit topHit;

            if (Physics.Raycast(
                topStart,
                Vector3.down,
                out topHit,
                maxLedgeHeight + 0.5f,
                ledgeMask))
            {
                float heightDifference =
                    topHit.point.y -
                    transform.position.y;

                // Check that the platform is reachable
                if (heightDifference > 0.5f &&
                    heightDifference <= maxLedgeHeight)
                {
                    GrabLedge(
                        wallHit,
                        topHit
                    );
                }
            }
        }
    }

    // =====================================================
    // GRAB LEDGE
    // =====================================================

    void GrabLedge(
        RaycastHit wallHit,
        RaycastHit topHit)
    {
        isHanging = true;
        if (animator != null)
{
    animator.SetBool("Hanging", true);
    animator.SetBool("Falling", false);
}

        isDashing = false;

        velocity = Vector3.zero;

        // Position the player in front of the ledge
        ledgeHangPosition =
            topHit.point
            - transform.forward * hangForwardOffset
            - Vector3.up * hangHeight;

        // Position where the player will stand after climbing
        ledgeClimbPosition =
            topHit.point
            + transform.forward * climbForwardOffset;

        // Stop the player
        controller.enabled = false;

        transform.position =
            ledgeHangPosition;

        controller.enabled = true;
    }

    // =====================================================
    // CLIMB UP
    // =====================================================

    IEnumerator ClimbUp()
{
    isClimbing = true;
    isHanging = false;

    if (animator != null)
    {
        animator.SetBool("Hanging", false);
        animator.SetTrigger("Climb");
    }

    velocity = Vector3.zero;
{
    animator.SetBool("Hanging", false);
}
        velocity = Vector3.zero;

        Vector3 startPosition =
            transform.position;

        float timer = 0f;

        while (timer < climbDuration)
        {
            timer += Time.deltaTime;

            float t =
                timer / climbDuration;

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            controller.enabled = false;

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    ledgeClimbPosition,
                    t
                );

            controller.enabled = true;

            yield return null;
        }

        controller.enabled = false;

        transform.position =
            ledgeClimbPosition;

        controller.enabled = true;

        isClimbing = false;

        // Reset movement
        velocity = Vector3.zero;

        jumpCount = 0;
        canDash = true;
    }

    // =====================================================
    // LET GO
    // =====================================================

    void LetGo()
{
    isHanging = false;

    if (animator != null)
    {
        animator.SetBool("Hanging", false);
    }

    ledgeCooldownTimer = ledgeGrabCooldown;

    velocity.y = -2f;

    controller.Move(-transform.forward * 0.15f);
}
}
