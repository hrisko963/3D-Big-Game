using UnityEngine;

public class LockOnIndicator : MonoBehaviour
{
    [Header("References")]
    public PlayerLockOn lockOn;
    public RectTransform indicator;
    public Camera mainCamera;

    [Header("Position")]
    public Vector3 screenOffset = new Vector3(0f, 30f, 0f);

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (lockOn == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                lockOn = player.GetComponent<PlayerLockOn>();
        }

        if (indicator != null)
            indicator.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (lockOn == null ||
            indicator == null ||
            mainCamera == null)
            return;

        if (!lockOn.IsLockedOn ||
            lockOn.LockOnPoint == null)
        {
            indicator.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                lockOn.LockOnPoint.position
            );

        // Enemy is behind the camera.
        if (screenPosition.z <= 0f)
        {
            indicator.gameObject.SetActive(false);
            return;
        }

        indicator.gameObject.SetActive(true);

        indicator.position =
            screenPosition +
            screenOffset;
    }
}