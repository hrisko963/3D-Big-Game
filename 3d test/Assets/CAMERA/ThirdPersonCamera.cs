using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    public float distance = 5f;
    public float height = 1.7f;

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

        yaw = transform.eulerAngles.y;
        smoothYaw = yaw;

        smoothPitch = pitch;

        smoothPivotPosition =
            target.position + Vector3.up * height;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // -------------------------
        // MOUSE INPUT
        // -------------------------

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        yaw += mouseX *
               mouseSensitivity *
               Time.deltaTime;

        if (invertY)
        {
            pitch += mouseY *
                     mouseSensitivity *
                     Time.deltaTime;
        }
        else
        {
            pitch -= mouseY *
                     mouseSensitivity *
                     Time.deltaTime;
        }

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );


        // -------------------------
        // SMOOTH CAMERA ROTATION
        // -------------------------

        smoothYaw = Mathf.SmoothDampAngle(
            smoothYaw,
            yaw,
            ref yawVelocity,
            rotationSmoothTime
        );

        smoothPitch = Mathf.SmoothDampAngle(
            smoothPitch,
            pitch,
            ref pitchVelocity,
            rotationSmoothTime
        );

        Quaternion cameraRotation =
            Quaternion.Euler(
                smoothPitch,
                smoothYaw,
                0f
            );


        // -------------------------
        // SMOOTH FOLLOW TARGET
        // -------------------------

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


        // -------------------------
        // CAMERA POSITION
        // -------------------------

        Vector3 desiredPosition =
            smoothPivotPosition -
            cameraRotation *
            Vector3.forward *
            distance;


        // -------------------------
        // CAMERA COLLISION
        // -------------------------

        Vector3 direction =
            desiredPosition -
            smoothPivotPosition;

        float desiredDistance =
            direction.magnitude;

        if (desiredDistance > 0.001f)
        {
            direction.Normalize();

            RaycastHit hit;

            if (Physics.SphereCast(
                smoothPivotPosition,
                collisionRadius,
                direction,
                out hit,
                desiredDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
            {
                desiredPosition =
                    smoothPivotPosition +
                    direction *
                    Mathf.Max(
                        hit.distance -
                        collisionOffset,
                        0.1f
                    );
            }
        }


        // -------------------------
        // APPLY CAMERA
        // -------------------------

        transform.position = desiredPosition;
        transform.rotation = cameraRotation;


        // -------------------------
        // CURSOR
        // -------------------------

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
}