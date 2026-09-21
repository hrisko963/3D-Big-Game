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
    public float positionSmoothTime = 0.05f;

    [Header("Camera Collision")]
    public LayerMask collisionMask;
    public float collisionRadius = 0.2f;
    public float collisionOffset = 0.1f;

    private float yaw;
    private float pitch = 15f;

    private Vector3 positionVelocity;


    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Camera target is not assigned!");
            return;
        }

        // Start behind the player's current direction
        yaw = target.eulerAngles.y;

        // Lock mouse
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

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseSensitivity * Time.deltaTime;

        if (invertY)
        {
            pitch += mouseY * mouseSensitivity * Time.deltaTime;
        }
        else
        {
            pitch -= mouseY * mouseSensitivity * Time.deltaTime;
        }

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );


        // -------------------------
        // CAMERA ROTATION
        // -------------------------

        Quaternion cameraRotation =
            Quaternion.Euler(pitch, yaw, 0f);


        // -------------------------
        // TARGET POSITION
        // -------------------------

        Vector3 pivotPosition =
            target.position +
            Vector3.up * height;

        Vector3 desiredPosition =
            pivotPosition -
            cameraRotation * Vector3.forward * distance;


        // -------------------------
        // CAMERA COLLISION
        // -------------------------

        Vector3 direction =
            desiredPosition - pivotPosition;

        float desiredDistance =
            direction.magnitude;

        direction.Normalize();

        RaycastHit hit;

        if (Physics.SphereCast(
            pivotPosition,
            collisionRadius,
            direction,
            out hit,
            desiredDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            desiredPosition =
                pivotPosition +
                direction *
                Mathf.Max(
                    hit.distance - collisionOffset,
                    0.1f
                );
        }


        // -------------------------
        // SMOOTH FOLLOW
        // -------------------------

        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime
            );

        transform.rotation = cameraRotation;


        // -------------------------
        // ESC = RELEASE MOUSE
        // -------------------------

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Left click locks it again
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}