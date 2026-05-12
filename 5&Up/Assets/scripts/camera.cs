using UnityEngine;

public class camera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object the camera orbits around.")]
    public Transform target;

    [Header("Camera Distance")]
    [Tooltip("Distance from the target.")]
    public float distance = 5f;

    [Tooltip("Minimum zoom distance.")]
    public float minDistance = 2f;

    [Tooltip("Maximum zoom distance.")]
    public float maxDistance = 20f;

    [Header("Rotation")]
    [Tooltip("Mouse sensitivity for rotation.")]
    public float mouseSensitivity = 2f;

    [Header("Height")]
    [Tooltip("Height offset from target.")]
    public float heightOffset = 2f;

    [Header("Smoothing")]
    [Tooltip("Smoothing for camera movement.")]
    public float smoothSpeed = 5f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        if (target == null)
        {
            target = transform.parent;
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleRotation();
        HandleZoom();
        UpdateCameraPosition();

        // Unlock cursor with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    void HandleRotation()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationY += mouseX;
        rotationX -= mouseY;

        // Clamp vertical rotation
        rotationX = Mathf.Clamp(rotationX, -60f, 60f);
    }

    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            distance -= scrollInput * 2f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    void UpdateCameraPosition()
    {
        if (target == null)
            return;

        // Calculate desired position based on rotation and distance
        Vector3 direction = new Vector3(
            Mathf.Sin(rotationY * Mathf.Deg2Rad) * Mathf.Cos(rotationX * Mathf.Deg2Rad),
            Mathf.Sin(rotationX * Mathf.Deg2Rad),
            -Mathf.Cos(rotationY * Mathf.Deg2Rad) * Mathf.Cos(rotationX * Mathf.Deg2Rad)
        ).normalized;

        Vector3 desiredPosition = target.position + direction * distance + Vector3.up * heightOffset;

        // Smooth camera movement
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Look at target
        transform.LookAt(target.position + Vector3.up * (heightOffset * 0.5f));
    }
}
