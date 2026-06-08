using UnityEngine;

public class camera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object the camera follows and orbits around.")]
    public Transform target;

    [Header("Camera Distance")]
    public float distance    = 5f;
    public float minDistance = 2f;
    public float maxDistance = 20f;

    [Header("Rotation")]
    public float mouseSensitivity = 2f;

    [Header("Height")]
    public float heightOffset = 2f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera moves to its desired position.")]
    public float smoothSpeed = 8f;

    [Tooltip("How quickly the camera blends between follow and freecam.")]
    public float transitionSpeed = 6f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    // Horizontal orbit angle — follow mode only rotates on this axis.
    private float yaw   = 0f;

    // Both angles used in freecam.
    private float freecamYaw   = 0f;
    private float freecamPitch = 10f;

    private bool  isFreecam     = false;
    private float blendT        = 0f;   // 0 = follow, 1 = freecam

    // Desired positions computed each frame — blended for smooth transition.
    private Vector3 followDesired;
    private Vector3 freecamDesired;

    void Start()
    {
        if (target == null)
            target = transform.parent;

        // Seed yaw from the camera's current angle so there's no snap on frame 1.
        yaw         = transform.eulerAngles.y;
        freecamYaw  = yaw;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (target == null) return;

        // Toggle freecam with right mouse button.
        if (Input.GetMouseButtonDown(1))
        {
            isFreecam = !isFreecam;

            // When entering freecam, inherit follow cam's current yaw so
            // it starts from the same view without a jump.
            if (isFreecam)
            {
                freecamYaw   = yaw;
                freecamPitch = 10f;
            }
        }

        // ESC toggles cursor lock.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }

        HandleInput();
        UpdatePositions();

        // Blend smoothly toward the active mode.
        float blendTarget = isFreecam ? 1f : 0f;
        blendT = Mathf.Lerp(blendT, blendTarget, transitionSpeed * Time.deltaTime);

        Vector3 desiredPos  = Vector3.Lerp(followDesired, freecamDesired, blendT);
        Vector3 lookPoint   = target.position + Vector3.up * heightOffset * 0.5f;

        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(lookPoint);
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    private void HandleInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (!isFreecam)
        {
            // Follow mode — horizontal orbit only.
            yaw += mouseX;

            // Scroll adjusts height offset in follow mode.
            if (Mathf.Abs(scroll) > 0.01f)
            {
                heightOffset -= scroll * 2f;
                heightOffset  = Mathf.Clamp(heightOffset, 0.5f, 8f);
            }
        }
        else
        {
            // Freecam — full orbit with both axes.
            freecamYaw   += mouseX;
            freecamPitch -= mouseY;
            freecamPitch  = Mathf.Clamp(freecamPitch, -60f, 60f);

            // Scroll adjusts distance in freecam.
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance -= scroll * 2f;
                distance  = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Compute desired positions for both modes every frame
    // -------------------------------------------------------------------------

    private void UpdatePositions()
    {
        // --- Follow mode ---
        // Orbits horizontally around the target, camera stays level.
        Quaternion followRot    = Quaternion.Euler(0f, yaw, 0f);
        Vector3    followOffset = followRot * new Vector3(0f, heightOffset, -distance);
        followDesired = target.position + followOffset;

        // --- Freecam ---
        // Full spherical orbit: pitch + yaw around the target.
        Quaternion freecamRot    = Quaternion.Euler(freecamPitch, freecamYaw, 0f);
        Vector3    freecamOffset = freecamRot * new Vector3(0f, 0f, -distance);
        freecamDesired = target.position + freecamOffset + Vector3.up * heightOffset * 0.5f;
    }
}