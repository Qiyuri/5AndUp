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
    [Tooltip("Seconds the player must be moving before auto-follow starts easing in.")]
    public float autoFollowDelay = 0.3f;
    [Tooltip("How quickly the auto-follow weight ramps from 0 to full once the delay is met.")]
    public float autoFollowRampSpeed = 3f;

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
    private bool    menuOpen     = false;

    private bool    playerIsMoving = false;

    // How long the player has been continuously moving.
    private float   movingTimer   = 0f;
    // Current blend weight of auto-follow (0 = off, 1 = full).
    private float   autoFollowWeight = 0f;

    private Vector3 previousTargetPosition;
    private float   smoothedSpeed = 0f;

    private Vector3 smoothedPosition;

    void Start()
    {
        if (target == null)
            target = transform.parent;

        yaw   = transform.eulerAngles.y;
        pitch = Mathf.Clamp(transform.eulerAngles.x, minPitch, maxPitch);

        previousTargetPosition = target != null ? target.position : Vector3.zero;
        smoothedPosition       = transform.position;

        SetCursorLock(false);
    }

    void LateUpdate()
    {
        if (target == null) return;

        float dt = Time.deltaTime;

        // ── Movement detection ────────────────────────────────────────────────
        Vector3 delta           = target.position - previousTargetPosition;
        Vector3 horizontalDelta = new Vector3(delta.x, 0f, delta.z);
        float   rawSpeed        = horizontalDelta.magnitude / Mathf.Max(dt, 0.0001f);
        smoothedSpeed = rawSpeed > smoothedSpeed
            ? Mathf.Lerp(smoothedSpeed, rawSpeed, 25f * dt)
            : Mathf.Lerp(smoothedSpeed, rawSpeed,  8f * dt);
        playerIsMoving = smoothedSpeed >= autoFollowThreshold;

        // ── Auto-follow weight: delay then ramp in, ramp out instantly ────────
        if (playerIsMoving)
        {
            movingTimer += dt;
            // Only start blending in once the player has been moving for 'autoFollowDelay' seconds.
            if (movingTimer >= autoFollowDelay)
                autoFollowWeight = Mathf.MoveTowards(autoFollowWeight, 1f, autoFollowRampSpeed * dt);
        }
        else
        {
            // Player stopped — reset immediately so the next movement starts fresh.
            movingTimer      = 0f;
            autoFollowWeight = 0f;
        }

        // ── Input ─────────────────────────────────────────────────────────────
        HandleCursorLock();
        HandleInput();

        // ── Auto-follow yaw ───────────────────────────────────────────────────
        // Weight goes 0→1 smoothly after the delay, so the camera eases behind
        // the player instead of snapping. Pitch and distance are never touched.
        if (autoFollowWeight > 0f)
        {
            Vector3 flatForward = new Vector3(target.forward.x, 0f, target.forward.z);
            if (flatForward.sqrMagnitude > 0.001f)
            {
                float targetYaw    = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
                float effectiveSpd = autoFollowSpeed * autoFollowWeight;
                yaw = Mathf.LerpAngle(yaw, targetYaw, effectiveSpd * dt);
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
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuOpen = !menuOpen;
            if (menuOpen)
                SetCursorLock(false);
        }

        if (!cursorLocked && !menuOpen && Input.GetMouseButtonDown(0))
            SetCursorLock(true);

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
    // Input
    // -------------------------------------------------------------------------

    private void HandleInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * 3f;
            distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        if (!cursorLocked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw   += mouseX;
        pitch -= mouseY;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Any manual mouse input resets the auto-follow timer so the camera
        // doesn't immediately fight the player's deliberate rotation.
        if (Mathf.Abs(mouseX) > 0.01f)
        {
            movingTimer      = 0f;
            autoFollowWeight = 0f;
        }
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