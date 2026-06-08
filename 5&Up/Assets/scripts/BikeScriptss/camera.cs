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
    [Tooltip("How quickly the camera snaps behind the player when moving.")]
    public float autoFollowSpeed = 3f;
    [Tooltip("How fast the target must be moving before auto-follow kicks in.")]
    public float autoFollowThreshold = 0.5f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera rotates to look at the target.")]
    public float rotationSmoothing = 10f;

    [Header("Manual Look")]
    [Tooltip("Vertical angle range when manually looking.")]
    public float minPitch = -30f;
    public float maxPitch =  60f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private float      yaw          = 0f;
    private float      pitch        = 10f;
    private bool       cursorLocked = false;
    private bool       isManualLook = false;   // True only while right mouse is held
    private Vector3    previousTargetPosition;
    private Quaternion smoothedRotation;
    private Quaternion desiredRotation;

    void Start()
    {
        if (target == null)
            target = transform.parent;

        yaw = transform.eulerAngles.y;
        previousTargetPosition = target != null ? target.position : Vector3.zero;

        smoothedRotation = transform.rotation;
        desiredRotation  = transform.rotation;

        SetCursorLock(false);
    }

    void FixedUpdate()
    {
        if (target == null) return;

        float dt = Time.fixedDeltaTime;

        Vector3 delta           = target.position - previousTargetPosition;
        Vector3 horizontalDelta = new Vector3(delta.x, 0f, delta.z);
        bool    isMoving        = horizontalDelta.magnitude / dt >= autoFollowThreshold;

        HandleCursorLock();
        HandleInput(isMoving);

        // Auto-follow only runs when the player is not manually looking.
        if (isMoving && !isManualLook)
            HandleAutoFollow(dt, horizontalDelta);

        ComputeDesired();

        // Apply smoothing only when NOT in manual look mode (for responsive feel while looking)
        if (!isManualLook)
        {
            smoothedRotation = Quaternion.Lerp(
                smoothedRotation, desiredRotation,
                rotationSmoothing * dt);
        }
        else
        {
            // No smoothing when manually looking — immediate response
            smoothedRotation = desiredRotation;
        }

        transform.position = ComputeDesiredPosition();
        transform.rotation = smoothedRotation;

        previousTargetPosition = target.position;
    }

    // -------------------------------------------------------------------------
    // Cursor lock — left click to lock, Tab or Esc to unlock
    // -------------------------------------------------------------------------

    private void HandleCursorLock()
    {
        if (!cursorLocked && Input.GetMouseButtonDown(0))
            SetCursorLock(true);

        if (cursorLocked && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)))
            SetCursorLock(false);
    }

    private void SetCursorLock(bool locked)
    {
        cursorLocked     = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    // -------------------------------------------------------------------------
    // Input — right mouse held = free look; released = back to auto-follow
    // -------------------------------------------------------------------------

    private void HandleInput(bool isMoving)
    {
        // Scroll zoom always works.
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * 3f;
            distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        if (!cursorLocked) return;

        // Right mouse button toggles manual look mode.
        if (Input.GetMouseButtonDown(1))
        {
            isManualLook = !isManualLook;
        }

        if (isManualLook)
        {
            // Full orbit — both axes — while in manual look mode.
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw   += mouseX;
            pitch -= mouseY;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
        // When exiting manual look mode, distance and pitch are preserved
        // and auto-follow will gradually take over.
    }

    // -------------------------------------------------------------------------
    // Auto-follow — eases yaw behind the target; pitch returns to neutral
    // -------------------------------------------------------------------------

    private void HandleAutoFollow(float dt, Vector3 horizontalDelta)
    {
        Vector3 targetForward = new Vector3(target.forward.x, 0f, target.forward.z).normalized;
        float   targetYaw     = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg;
        yaw = Mathf.LerpAngle(yaw, targetYaw, autoFollowSpeed * dt);

        // Ease pitch back to a neutral angle so the view isn't stuck tilted.
        pitch = Mathf.Lerp(pitch, 10f, autoFollowSpeed * dt);
    }

    // -------------------------------------------------------------------------
    // Position and rotation helpers
    // -------------------------------------------------------------------------

    private Vector3 ComputeDesiredPosition()
    {
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3    orbitOffset   = orbitRotation * new Vector3(0f, 0f, -distance);
        return target.position + orbitOffset + Vector3.up * heightOffset;
    }

    private void ComputeDesired()
    {
        Vector3 desiredPos = ComputeDesiredPosition();
        Vector3 lookPoint  = target.position + Vector3.up * heightOffset * 0.5f;
        desiredRotation    = Quaternion.LookRotation(lookPoint - desiredPos, Vector3.up);
    }
}