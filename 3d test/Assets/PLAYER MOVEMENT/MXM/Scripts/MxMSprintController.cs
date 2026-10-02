using UnityEngine;
using MxM;

public class MxMSprintController : MonoBehaviour
{
    [Header("References")]
    public MxMTrajectoryGenerator trajectoryGenerator;

    [Header("Speeds")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;

    [Header("Transition")]
    public float acceleration = 12f;
    public float deceleration = 20f;

    private float currentSpeed;

    void Awake()
    {
        if (trajectoryGenerator == null)
            trajectoryGenerator = GetComponent<MxMTrajectoryGenerator>();

        currentSpeed = walkSpeed;
    }

    void Update()
    {
        if (trajectoryGenerator == null)
            return;

        bool hasMovementInput =
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f ||
            Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;

        bool sprintHeld =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        float targetSpeed;

        if (hasMovementInput && sprintHeld)
            targetSpeed = runSpeed;
        else
            targetSpeed = walkSpeed;

        float changeSpeed =
            targetSpeed > currentSpeed ? acceleration : deceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            changeSpeed * Time.deltaTime
        );

        trajectoryGenerator.MaxSpeed = currentSpeed;
    }
}