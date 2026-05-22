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

    [Header("Freecam")]
    [Tooltip("Speed of freecam movement.")]
    public float freecamSpeed = 5f;

    private float rotationX = 0f;
    private float rotationY = 0f;
    private bool isFreecam = false;

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
        // Toggle freecam with right mouse button
        if (Input.GetMouseButtonDown(1))
        {
            isFreecam = !isFreecam;
        }

        if (isFreecam)
        {
            HandleFreecam();
        }
        else
        {
            HandleFollowCam();
        }

        // Unlock cursor with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    void HandleFreecam()
    {
        // Handle orbital camera rotation and zoom
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            rotationY += mouseX;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -60f, 60f);
        }

        // Handle zoom
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            distance -= scrollInput * 2f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        if (target == null)
            return;

        // Calculate orbital position around target
        Vector3 direction = new Vector3(
            Mathf.Sin(rotationY * Mathf.Deg2Rad) * Mathf.Cos(rotationX * Mathf.Deg2Rad),
            Mathf.Sin(rotationX * Mathf.Deg2Rad),
            -Mathf.Cos(rotationY * Mathf.Deg2Rad) * Mathf.Cos(rotationX * Mathf.Deg2Rad)
        ).normalized;

        Vector3 desiredPosition = target.position + direction * distance + Vector3.up * heightOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * (heightOffset * 0.5f));
    }

    void HandleFollowCam()
    {
        if (target == null)
            return;

        // Handle vertical height adjustment with scroll wheel
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            heightOffset -= scrollInput * 2f;
            heightOffset = Mathf.Clamp(heightOffset, 0.5f, 5f);
        }

        // Follow target position with player's rotation
        Vector3 desiredPosition = target.position - target.forward * distance + Vector3.up * heightOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Only match target's yaw (left/right rotation), not pitch/roll
        float targetYaw = target.eulerAngles.y;
        Quaternion desiredRotation = Quaternion.Euler(transform.eulerAngles.x, targetYaw, 0f);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, smoothSpeed * Time.deltaTime);
        
        transform.LookAt(target.position + Vector3.up * (heightOffset * 0.5f));
    }
}
