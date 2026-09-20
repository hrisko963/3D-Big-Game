
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

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        lastPosition = transform.position;
    }

    void Update()
    {
        // Calculate actual horizontal movement speed
        Vector3 movement = transform.position - lastPosition;
        movement.y = 0;

        float currentSpeed = movement.magnitude / Time.deltaTime;

        lastPosition = transform.position;

        // -------------------------
        // WALK SPEED
        // -------------------------

        float walkAmount = Mathf.Clamp01(
            currentSpeed / walkSpeed
        );

        // -------------------------
        // RUN SPEED
        // -------------------------

        float runAmount = Mathf.Clamp01(
            currentSpeed / runSpeed
        );

        // Only use walking float when NOT running
        bool isRunning =
            Input.GetKey(KeyCode.LeftShift) &&
            controller.isGrounded &&
            currentSpeed > walkSpeed;

        if (isRunning)
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
        // GROUNDED
        // -------------------------

        animator.SetBool(
            "Grounded",
            controller.isGrounded
        );

        // -------------------------
        // FALLING
        // -------------------------

        if (!controller.isGrounded && movement.y < -0.1f)
        {
            animator.SetBool("Falling", true);
        }
        else
        {
            animator.SetBool("Falling", false);
        }

        // -------------------------
        // JUMP
        // -------------------------

        if (Input.GetKeyDown(KeyCode.Space) &&
            controller.isGrounded)
        {
            animator.SetTrigger("Jump");
        }

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
