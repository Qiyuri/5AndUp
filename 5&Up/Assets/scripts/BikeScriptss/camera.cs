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
    [Tooltip("How fast the target must be moving before the camera starts auto-following.")]
    public float autoFollowThreshold = 0.5f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera rotates to look at the target.")]
    public float rotationSmoothing = 10f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private float      yaw          = 0f;
    private bool       cursorLocked = false;
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

        HandleCursorLock();
        HandleInput();
        HandleAutoFollow(dt);
        ComputeDesired();

        // Apply light smoothing to reduce jitter
        smoothedRotation = Quaternion.Lerp(
            smoothedRotation, desiredRotation,
            rotationSmoothing * dt);

        // Position is set directly with no smoothing.
        transform.position = ComputeDesiredPosition();
        transform.rotation = smoothedRotation;

        previousTargetPosition = target.position;
    }

    // -------------------------------------------------------------------------
    // Cursor lock — click to lock, Tab or Esc to unlock
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
    // Input — horizontal orbit + scroll zoom
    // -------------------------------------------------------------------------

    private void HandleInput()
    {
        if (!cursorLocked) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * 3f;
            distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    // -------------------------------------------------------------------------
    // Auto-follow — rotates yaw to stay behind the target based on target's facing direction
    // -------------------------------------------------------------------------

    private void HandleAutoFollow(float dt)
    {
        if (target == null) return;

        Vector3 delta           = target.position - previousTargetPosition;
        Vector3 horizontalDelta = new Vector3(delta.x, 0f, delta.z);

        // Only auto-follow if the character is moving fast enough
        if (horizontalDelta.magnitude / dt < autoFollowThreshold)
            return;

        // Use the target's forward direction
        Vector3 targetForward = new Vector3(target.forward.x, 0f, target.forward.z).normalized;
        float targetYaw = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg;
        
        // Smoothly rotate to face the target's direction
        yaw = Mathf.LerpAngle(yaw, targetYaw, autoFollowSpeed * dt);
    }

    // -------------------------------------------------------------------------
    // Position and rotation helpers
    // -------------------------------------------------------------------------

    private Vector3 ComputeDesiredPosition()
    {
        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3    orbitOffset = yawRotation * new Vector3(0f, heightOffset, -distance);
        return target.position + orbitOffset;
    }

    private void ComputeDesired()
    {
        Vector3 desiredPos = ComputeDesiredPosition();
        Vector3 lookPoint  = target.position + Vector3.up * heightOffset * 0.5f;
        desiredRotation    = Quaternion.LookRotation(lookPoint - desiredPos, Vector3.up);
    }
}