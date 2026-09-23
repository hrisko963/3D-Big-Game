using UnityEngine;
using System.Collections;

public class PlayerDodge : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Animator animator;
    public Transform cameraTransform;
    public PlayerMovement playerMovement;
    public PlayerLockOn lockOn;

    [Header("Dodge")]
    public KeyCode dodgeKey = KeyCode.LeftAlt;
    public float dodgeDistance = 4f;
    public float dodgeDuration = 0.45f;
    public float dodgeCooldown = 0.7f;

    private bool isDodging;
    private float nextDodgeTime;

    public bool IsDodging
    {
        get { return isDodging; }
    }

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (lockOn == null)
            lockOn = GetComponent<PlayerLockOn>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (isDodging)
            return;

        if (Time.time < nextDodgeTime)
            return;

        if (Input.GetKeyDown(dodgeKey))
            StartDodge();
    }

    void StartDodge()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 dodgeDirection;
        string dodgeTrigger;

        // LEFT
        if (horizontal < -0.1f)
        {
            dodgeDirection = GetCameraRight() * -1f;
            dodgeTrigger = "DodgeLeft";
        }

        // RIGHT
        else if (horizontal > 0.1f)
        {
            dodgeDirection = GetCameraRight();
            dodgeTrigger = "DodgeRight";
        }

        // FORWARD
        else if (vertical > 0.1f)
        {
            dodgeDirection = GetCameraForward();
            dodgeTrigger = "DodgeForward";
        }

        // BACKWARD or no movement input
        else
        {
            dodgeDirection = GetCameraForward() * -1f;
            dodgeTrigger = "DodgeBackward";
        }

        StartCoroutine(
            DodgeRoutine(
                dodgeDirection.normalized,
                dodgeTrigger
            )
        );
    }

    Vector3 GetCameraForward()
    {
        if (cameraTransform == null)
            return transform.forward;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;

        return forward.normalized;
    }

    Vector3 GetCameraRight()
    {
        if (cameraTransform == null)
            return transform.right;

        Vector3 right = cameraTransform.right;
        right.y = 0f;

        return right.normalized;
    }

    IEnumerator DodgeRoutine(
        Vector3 direction,
        string animationTrigger)
    {
        isDodging = true;

        nextDodgeTime =
            Time.time + dodgeCooldown;

        // Stop normal movement during dodge.
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (animator != null)
        {
            animator.SetFloat("WalkSpeed", 0f);
            animator.SetFloat("RunSpeed", 0f);

            animator.ResetTrigger("DodgeForward");
            animator.ResetTrigger("DodgeBackward");
            animator.ResetTrigger("DodgeLeft");
            animator.ResetTrigger("DodgeRight");

            animator.SetTrigger(animationTrigger);
        }

        float timer = 0f;

        float dodgeSpeed =
            dodgeDistance / dodgeDuration;

        while (timer < dodgeDuration)
        {
            if (controller != null)
            {
                controller.Move(
                    direction *
                    dodgeSpeed *
                    Time.deltaTime
                );
            }

            timer += Time.deltaTime;

            yield return null;
        }

        if (playerMovement != null)
            playerMovement.enabled = true;

        isDodging = false;
    }
}