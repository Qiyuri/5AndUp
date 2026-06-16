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

    [Header("Follow Mode")]
    [Tooltip("Hoe snel de camera yaw achter de speler aankomt in follow mode.")]
    public float followSnapSpeed = 12f;
    [Tooltip("Hoe snel de pitch terugkomt naar de standaard pitch in follow mode.")]
    public float followPitchSnapSpeed = 4f;
    [Tooltip("Standaard pitch in follow mode (hoek omhoog).")]
    public float followDefaultPitch = 10f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera position smooths toward its desired spot.")]
    public float positionSmoothing = 10f;

    [Header("Freecam")]
    [Tooltip("Vertical angle range in freecam.")]
    public float minPitch = -30f;
    public float maxPitch =  70f;

    // ── Modi ─────────────────────────────────────────────────────────────────
    private enum CameraMode { Follow, Freecam }
    private CameraMode mode = CameraMode.Follow;

    // ── Private state ─────────────────────────────────────────────────────────
    private float   yaw;
    private float   pitch        = 10f;
    private bool    cursorLocked = false;
    private bool    menuOpen     = false;

    private Vector3 smoothedPosition;

    void Start()
    {
        if (target == null)
            target = transform.parent;

        yaw   = transform.eulerAngles.y;
        pitch = followDefaultPitch;

        smoothedPosition = transform.position;

        SetCursorLock(false);
    }

    void LateUpdate()
    {
        if (target == null) return;

        float dt = Time.deltaTime;

        // ── Modus wisselen met L ──────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.L))
        {
            mode = (mode == CameraMode.Follow) ? CameraMode.Freecam : CameraMode.Follow;
            Debug.Log($"[Camera] Modus: {mode}");
        }

        // ── Cursor lock ───────────────────────────────────────────────────────
        HandleCursorLock();

        // ── Input per modus ───────────────────────────────────────────────────
        HandleScrollZoom();

        if (mode == CameraMode.Follow)
            HandleFollowMode(dt);
        else
            HandleFreecamMode(dt);

        // ── Positie berekenen & smoothen ──────────────────────────────────────
        Vector3    desiredPos = ComputeDesiredPosition();
        Vector3    lookPoint  = target.position + Vector3.up * heightOffset * 0.5f;
        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - desiredPos, Vector3.up);

        smoothedPosition   = Vector3.Lerp(smoothedPosition, desiredPos, positionSmoothing * dt);
        transform.position = smoothedPosition;
        transform.rotation = desiredRot;
    }

    // ── Follow mode ───────────────────────────────────────────────────────────
    // Yaw volgt altijd direct de bike. Alleen verticale muis (pitch) en scroll
    // zijn beschikbaar.
    private void HandleFollowMode(float dt)
    {
        // Yaw: altijd achter de bike — geen vertraging, geen delay
        Vector3 flatForward = new Vector3(target.forward.x, 0f, target.forward.z);
        if (flatForward.sqrMagnitude > 0.001f)
        {
            float targetYaw = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            yaw = Mathf.LerpAngle(yaw, targetYaw, followSnapSpeed * dt);
        }

        // Pitch: verticale muis input, cursor moet gelockt zijn
        if (cursorLocked)
        {
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch -= mouseY;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    // ── Freecam mode ──────────────────────────────────────────────────────────
    // Volledige muis controle over yaw en pitch.
    private void HandleFreecamMode(float dt)
    {
        if (!cursorLocked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw   += mouseX;
        pitch -= mouseY;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    // ── Scroll zoom (beide modi) ──────────────────────────────────────────────
    private void HandleScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * 3f;
            distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    // ── Cursor lock ───────────────────────────────────────────────────────────
    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuOpen = !menuOpen;
            if (menuOpen) SetCursorLock(false);
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

    // ── Positie berekening ────────────────────────────────────────────────────
    private Vector3 ComputeDesiredPosition()
    {
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3    orbitOffset   = orbitRotation * new Vector3(0f, 0f, -distance);
        return target.position + orbitOffset + Vector3.up * heightOffset;
    }
}