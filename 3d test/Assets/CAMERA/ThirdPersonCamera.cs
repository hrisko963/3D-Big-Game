using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Lock On")]
    public PlayerLockOn lockOn;
    public float lockOnRotationSpeed = 5f;
    public float lockOnHeightOffset = 1.2f;

    [Header("Camera Position")]
    public float distance = 5f;
    public float height = 1.7f;

    [Header("Shoulder Camera")]
    public float shoulderOffset = 0.8f;

    [Header("Mouse")]
    public float mouseSensitivity = 180f;
    public bool invertY = false;

    [Header("Vertical Look")]
    public float minPitch = -25f;
    public float maxPitch = 60f;

    [Header("Smoothing")]
    public float followSmoothTime = 0.08f;
    public float rotationSmoothTime = 0.04f;

    [Header("Camera Collision")]
    public LayerMask collisionMask;
    public float collisionRadius = 0.2f;
    public float collisionOffset = 0.1f;

    private float yaw;
    private float pitch = 15f;

    private float smoothYaw;
    private float smoothPitch;

    private float yawVelocity;
    private float pitchVelocity;

    private Vector3 smoothPivotPosition;
    private Vector3 pivotVelocity;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Camera target is not assigned!");
            return;
        }

        if (lockOn == null)
            lockOn = target.GetComponent<PlayerLockOn>();

        yaw = transform.eulerAngles.y;

        smoothYaw = yaw;
        smoothPitch = pitch;

        smoothPivotPosition =
            target.position +
            Vector3.up * height;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // FREE CAMERA OR LOCK-ON CAMERA
        if (lockOn != null &&
            lockOn.IsLockedOn &&
            lockOn.CurrentTarget != null)
        {
            UpdateLockOnCamera();
        }
        else
        {
            UpdateFreeCamera();
        }

        Quaternion cameraRotation =
            Quaternion.Euler(
                smoothPitch,
                smoothYaw,
                0f
            );

        // Player camera pivot
        Vector3 desiredPivotPosition =
            target.position +
            Vector3.up * height;

        smoothPivotPosition =
            Vector3.SmoothDamp(
                smoothPivotPosition,
                desiredPivotPosition,
                ref pivotVelocity,
                followSmoothTime
            );

        // RIGHT SHOULDER OFFSET
        Vector3 shoulderDirection =
            cameraRotation * Vector3.right;

        Vector3 cameraPivot =
            smoothPivotPosition +
            shoulderDirection * shoulderOffset;

        // Put camera behind shoulder
        Vector3 desiredPosition =
            cameraPivot -
            cameraRotation *
            Vector3.forward *
            distance;

        // CAMERA COLLISION
        Vector3 direction =
            desiredPosition -
            cameraPivot;

        float desiredDistance =
            direction.magnitude;

        if (desiredDistance > 0.001f)
        {
            direction.Normalize();

            RaycastHit hit;

            if (Physics.SphereCast(
                cameraPivot,
                collisionRadius,
                direction,
                out hit,
                desiredDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
            {
                desiredPosition =
                    cameraPivot +
                    direction *
                    Mathf.Max(
                        hit.distance - collisionOffset,
                        0.1f
                    );
            }
        }

        transform.position = desiredPosition;
        transform.rotation = cameraRotation;

        // CURSOR
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }
    }

    void UpdateFreeCamera()
    {
        float mouseX =
            Input.GetAxisRaw("Mouse X");

        float mouseY =
            Input.GetAxisRaw("Mouse Y");

        yaw +=
            mouseX *
            mouseSensitivity *
            Time.deltaTime;

        if (invertY)
        {
            pitch +=
                mouseY *
                mouseSensitivity *
                Time.deltaTime;
        }
        else
        {
            pitch -=
                mouseY *
                mouseSensitivity *
                Time.deltaTime;
        }

        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );

        smoothYaw =
            Mathf.SmoothDampAngle(
                smoothYaw,
                yaw,
                ref yawVelocity,
                rotationSmoothTime
            );

        smoothPitch =
            Mathf.SmoothDampAngle(
                smoothPitch,
                pitch,
                ref pitchVelocity,
                rotationSmoothTime
            );
    }

    void UpdateLockOnCamera()
{
    Transform enemy = lockOn.LockOnPoint;

    if (enemy == null)
        return;

    // Point at the enemy's upper body.
    Vector3 enemyPosition = enemy.position;

    // Direction from player toward enemy.
    Vector3 direction =
        enemyPosition -
        smoothPivotPosition;

    if (direction.sqrMagnitude < 0.01f)
        return;

    Quaternion lookRotation =
        Quaternion.LookRotation(direction);

    float targetYaw =
        lookRotation.eulerAngles.y;

    float targetPitch =
        NormalizeAngle(
            lookRotation.eulerAngles.x
        );

    targetPitch =
        Mathf.Clamp(
            targetPitch,
            minPitch,
            maxPitch
        );

    // Stay locked directly onto the enemy.
    smoothYaw = targetYaw;
    smoothPitch = targetPitch;

    // Keep free-camera values synchronized.
    // This prevents snapping when we unlock.
    yaw = smoothYaw;
    pitch = smoothPitch;
}

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}