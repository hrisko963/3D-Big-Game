using UnityEngine;

public class MouseCameraControl : MonoBehaviour
{
    public float sensitivity = 200f;
    float xRotation = 0f;

    void Start()
    {
        // Hide and lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get mouse movement inputs scaled by time and sensitivity
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        // Control vertical rotation (looking up and down) and clamp it
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply vertical rotation to the camera locally
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotate the parent or player horizontally left and right (using mouseX)
        transform.parent.Rotate(Vector3.up * mouseX);
    }
}