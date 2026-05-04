using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("Look Settings")]
    public Transform playerBody;
    public float mouseSensitivity = 100f;
    public float topClamp = 85f;
    public float bottomClamp = -85f;

    private float verticalRotation = 0f;

    void Start()
    {
        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleLook();
    }

    private void HandleLook()
    {
        if (playerBody == null) return;

        // Get Mouse Input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate Player Body (Yaw)
        playerBody.Rotate(Vector3.up * mouseX);

        // Rotate Camera (Pitch)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, bottomClamp, topClamp);
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}
