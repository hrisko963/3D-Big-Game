using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Player Movement")]
    public PlayerMovement playerMovement;

    [Header("Lock On")]
    public PlayerLockOn lockOn;
    public float lockOnRotationSpeed = 5f;
    public float lockOnHeightOffset = 1.2f;

    [Header("Camera Position")]
    public float distance = 5f;
    public float height = 1.7f;

    [Header("Crouch Camera")]
    public float crouchHeight = 1.05f;
    public float crouchCameraSmoothTime = 0.12f;

    [Header("Running Footstep Bob")]
    [Tooltip("Vertical amount of the running camera bob.")]
    public float runBobVerticalAmount = 0.045f;

    [Tooltip("Side-to-side amount of the running camera bob.")]
    public float runBobHorizontalAmount = 0.025f;

    [Tooltip("Speed/frequency of the running footsteps.")]
    public float runBobSpeed = 11f;

    [Tooltip("How smoothly the bob fades in and out.")]
    public float runBobFadeSpeed = 7f;

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


    // ==================================================
    // ROTATION
    // ==================================================

    private float yaw;
    private float pitch = 15f;

    private float smoothYaw;
    private float smoothPitch;

    private float yawVelocity;
    private float pitchVelocity;


    // ==================================================
    // POSITION
    // ==================================================

    private Vector3 smoothPivotPosition;
    private Vector3 pivotVelocity;


    // ==================================================
    // CROUCH CAMERA
    // ==================================================

    private float currentCameraHeight;
    private float cameraHeightVelocity;


    // ==================================================
    // RUNNING BOB
    // ==================================================

    private float runBobTimer;
    private float currentRunBobStrength;


    // ==================================================
    // START
    // ==================================================

    void Start()
    {
        if (target == null)
        {
            Debug.LogError(
                "Camera target is not assigned!"
            );

            return;
        }


        if (lockOn == null)
        {
            lockOn =
                target.GetComponent<PlayerLockOn>();
        }


        if (playerMovement == null)
        {
            playerMovement =
                target.GetComponent<PlayerMovement>();
        }


        yaw = transform.eulerAngles.y;

        smoothYaw = yaw;
        smoothPitch = pitch;


        currentCameraHeight = height;


        smoothPivotPosition =
            target.position +
            Vector3.up *
            currentCameraHeight;


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }


    // ==================================================
    // LATE UPDATE
    // ==================================================

    void LateUpdate()
    {
        if (target == null)
            return;


        // ==================================================
        // CAMERA HEIGHT
        // ==================================================

        UpdateCameraHeight();


        // ==================================================
        // CAMERA ROTATION
        // ==================================================

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


        // ==================================================
        // PLAYER PIVOT
        // ==================================================

        Vector3 desiredPivotPosition =
            target.position +
            Vector3.up *
            currentCameraHeight;


        smoothPivotPosition =
            Vector3.SmoothDamp(
                smoothPivotPosition,
                desiredPivotPosition,
                ref pivotVelocity,
                followSmoothTime
            );


        // ==================================================
        // RUNNING FOOTSTEP BOB
        // ==================================================

        Vector3 runBobOffset =
            UpdateRunningBob(
                cameraRotation
            );


        // ==================================================
        // SHOULDER
        // ==================================================

        Vector3 shoulderDirection =
            cameraRotation *
            Vector3.right;


        Vector3 cameraPivot =
            smoothPivotPosition +
            shoulderDirection *
            shoulderOffset +
            runBobOffset;


        // ==================================================
        // CAMERA POSITION
        // ==================================================

        Vector3 desiredPosition =
            cameraPivot -
            cameraRotation *
            Vector3.forward *
            distance;


        // ==================================================
        // CAMERA COLLISION
        // ==================================================

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
                        hit.distance -
                        collisionOffset,
                        0.1f
                    );
            }
        }


        // ==================================================
        // APPLY CAMERA
        // ==================================================

        transform.position =
            desiredPosition;

        transform.rotation =
            cameraRotation;


        // ==================================================
        // CURSOR
        // ==================================================

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
    }


    // ==================================================
    // RUNNING FOOTSTEP BOB
    // ==================================================

    Vector3 UpdateRunningBob(
        Quaternion cameraRotation)
    {
        bool shouldBob =
            playerMovement != null &&
            playerMovement.IsRunning &&
            playerMovement.IsGrounded &&
            playerMovement.HasMovementInput &&
            !playerMovement.IsCrouching &&
            !playerMovement.IsSliding;


        // Fade bob smoothly in/out.
        float targetStrength =
            shouldBob ? 1f : 0f;


        currentRunBobStrength =
            Mathf.MoveTowards(
                currentRunBobStrength,
                targetStrength,
                runBobFadeSpeed *
                Time.deltaTime
            );


        // Advance footstep rhythm only
        // while actually running.
        if (shouldBob)
        {
            runBobTimer +=
                Time.deltaTime *
                runBobSpeed;
        }


        // No noticeable bob = return zero.
        if (currentRunBobStrength <= 0.001f)
        {
            // Reset so the next sprint begins
            // from a predictable position.
            runBobTimer = 0f;

            return Vector3.zero;
        }


        // ==================================================
        // FOOTSTEP RHYTHM
        // ==================================================
        //
        // Vertical uses twice the frequency.
        // This gives us a vertical movement for
        // each left AND right footstep.
        //
        // Horizontal uses the base frequency,
        // giving a gentle left/right weight shift.
        // ==================================================

        float verticalBob =
            Mathf.Sin(
                runBobTimer * 2f
            ) *
            runBobVerticalAmount;


        float horizontalBob =
            Mathf.Sin(
                runBobTimer
            ) *
            runBobHorizontalAmount;


        verticalBob *=
            currentRunBobStrength;

        horizontalBob *=
            currentRunBobStrength;


        // Horizontal movement follows the
        // camera's local right direction.
        Vector3 cameraRight =
            cameraRotation *
            Vector3.right;


        Vector3 offset =
            cameraRight *
            horizontalBob;


        // Vertical stays world-up so the
        // camera doesn't bob diagonally
        // when looking upward/downward.
        offset +=
            Vector3.up *
            verticalBob;


        return offset;
    }


    // ==================================================
    // CROUCH CAMERA HEIGHT
    // ==================================================

    void UpdateCameraHeight()
    {
        float targetHeight =
            height;


        if (playerMovement != null &&
            (playerMovement.IsCrouching ||
             playerMovement.IsSliding))
        {
            targetHeight =
                crouchHeight;
        }


        currentCameraHeight =
            Mathf.SmoothDamp(
                currentCameraHeight,
                targetHeight,
                ref cameraHeightVelocity,
                crouchCameraSmoothTime
            );
    }


    // ==================================================
    // FREE CAMERA
    // ==================================================

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


    // ==================================================
    // LOCK-ON CAMERA
    // ==================================================

    void UpdateLockOnCamera()
    {
        Transform enemy =
            lockOn.LockOnPoint;


        if (enemy == null)
            return;


        Vector3 enemyPosition =
            enemy.position;


        Vector3 direction =
            enemyPosition -
            smoothPivotPosition;


        if (direction.sqrMagnitude < 0.01f)
            return;


        Quaternion lookRotation =
            Quaternion.LookRotation(
                direction
            );


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


        smoothYaw =
            targetYaw;

        smoothPitch =
            targetPitch;


        // Prevent snapping when unlocking.
        yaw = smoothYaw;
        pitch = smoothPitch;
    }


    // ==================================================
    // NORMALIZE ANGLE
    // ==================================================

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}