using UnityEngine;

public class camera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object the camera follows.")]
    public Transform target;

    [Header("Distance")]
    public float distance    = 6f;
    public float minDistance = 2f;
    public float maxDistance = 15f;

    [Header("Height")]
    [Tooltip("How high above the target the camera sits.")]
    public float heightOffset = 2f;

    [Header("Rotation")]
    public float mouseSensitivity = 2f;

    [Header("Auto-Follow")]
    [Tooltip("How quickly the camera yaw snaps behind the player when moving.")]
    public float autoFollowSpeed = 5f;
    [Tooltip("How fast the target must be moving (m/s) before auto-follow kicks in.")]
    public float autoFollowThreshold = 0.5f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera position smooths toward its desired spot.")]
    public float positionSmoothing = 10f;

    [Header("Manual Look")]
    [Tooltip("Vertical angle range when manually looking.")]
    public float minPitch = -30f;
    public float maxPitch =  70f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private float   yaw;
    private float   pitch        = 10f;
    private bool    cursorLocked = false;
    private bool    menuOpen     = false;   // Track if menu is open

    // True the moment the player starts moving; false again once they stop.
    private bool    playerIsMoving = false;

    private Vector3 previousTargetPosition;
    private float   smoothedSpeed = 0f;   // low-pass filtered speed to avoid jitter

    // Smoothed world position of the camera.
    private Vector3 smoothedPosition;

    void Start()
    {
        if (target == null)
            target = transform.parent;

        yaw = transform.eulerAngles.y;
        pitch = Mathf.Clamp(transform.eulerAngles.x, minPitch, maxPitch);

        previousTargetPosition = target != null ? target.position : Vector3.zero;
        smoothedPosition       = transform.position;

        SetCursorLock(false);
    }

    void LateUpdate()
    {
        if (target == null) return;

        float dt = Time.deltaTime;

        // ── Movement detection (smoothed to avoid floating-point jitter) ────────
        Vector3 delta           = target.position - previousTargetPosition;
        Vector3 horizontalDelta = new Vector3(delta.x, 0f, delta.z);
        float   rawSpeed        = horizontalDelta.magnitude / Mathf.Max(dt, 0.0001f);
        // Low-pass filter: rises quickly, falls slowly — avoids 1-frame spikes
        smoothedSpeed   = rawSpeed > smoothedSpeed
            ? Mathf.Lerp(smoothedSpeed, rawSpeed, 25f * dt)   // fast rise
            : Mathf.Lerp(smoothedSpeed, rawSpeed,  8f * dt);  // slow fall
        playerIsMoving  = smoothedSpeed >= autoFollowThreshold;

        // ── Input ─────────────────────────────────────────────────────────────
        HandleCursorLock();
        HandleInput();

        // ── Auto-follow yaw only while moving ─────────────────────────────────
        // Pitch and distance are NEVER touched by auto-follow — they stay
        // exactly where the player left them.
        if (playerIsMoving)
        {
            Vector3 flatForward = new Vector3(target.forward.x, 0f, target.forward.z);
            if (flatForward.sqrMagnitude > 0.001f)
            {
                float targetYaw = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
                yaw = Mathf.LerpAngle(yaw, targetYaw, autoFollowSpeed * dt);
            }
        }

        // ── Compute desired position & rotation ───────────────────────────────
        Vector3    desiredPos = ComputeDesiredPosition();
        Vector3    lookPoint  = target.position + Vector3.up * heightOffset * 0.5f;
        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - desiredPos, Vector3.up);

        // ── Smooth position, snap rotation ────────────────────────────────────
        smoothedPosition = Vector3.Lerp(smoothedPosition, desiredPos, positionSmoothing * dt);

        transform.position = smoothedPosition;
        transform.rotation = desiredRot;

        previousTargetPosition = target.position;
    }

    // -------------------------------------------------------------------------
    // Cursor lock
    // -------------------------------------------------------------------------

    private void HandleCursorLock()
    {
        // Toggle menu state when Tab is pressed
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuOpen = !menuOpen;
            // Unlock cursor when menu opens
            if (menuOpen)
                SetCursorLock(false);
        }

        // Can only lock cursor if menu is closed
        if (!cursorLocked && !menuOpen && Input.GetMouseButtonDown(0))
            SetCursorLock(true);

        // Unlock with Escape (menu stays in its current state)
        if (cursorLocked && Input.GetKeyDown(KeyCode.Escape))
            SetCursorLock(false);
    }

    private void SetCursorLock(bool locked)
    {
        cursorLocked     = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    // -------------------------------------------------------------------------
    // Input — mouse always rotates freely; scroll always zooms
    // -------------------------------------------------------------------------

    private void HandleInput()
    {
        // Zoom — always works.
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * 3f;
            distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        if (!cursorLocked) return;

        // Mouse drag always orbits the camera freely (both axes).
        // Auto-follow will ease yaw back behind the player once they move,
        // but pitch and distance are always preserved.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw   += mouseX;
        pitch -= mouseY;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    // -------------------------------------------------------------------------
    // Position helper
    // -------------------------------------------------------------------------

    private Vector3 ComputeDesiredPosition()
    {
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3    orbitOffset   = orbitRotation * new Vector3(0f, 0f, -distance);
        return target.position + orbitOffset + Vector3.up * heightOffset;
    }
}