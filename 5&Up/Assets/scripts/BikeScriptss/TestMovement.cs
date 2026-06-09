using UnityEngine;

public class TestMovement : MonoBehaviour
{
    [Header("Left Bike Wheels")]
    public Transform leftBike_frontWheel;
    public Transform leftBike_rearWheel;

    [Header("Right Bike Wheels")]
    public Transform rightBike_frontWheel;
    public Transform rightBike_rearWheel;

    [Header("Drive")]
    public float driveForce   = 70f;
    public float reverseForce = 50f;
    public float maxSpeed     = 20f;

    [Header("Steering")]
    public float maxYawSpeed    = 120f;   // deg/s
    public float steerSnapSpeed = 14f;    // how fast yaw velocity snaps to target
    public float maxLeanAngle   = 30f;
    public float leanSnapSpeed  = 10f;    // how fast lean catches up

    [Header("Step-Up")]
    [Tooltip("How far in front of the bike to check for obstacles to climb.")]
    public float stepCheckDistance = 0.5f;
    [Tooltip("Max height of a step the bike can automatically climb.")]
    public float maxStepHeight     = 0.4f;
    [Tooltip("Upward force applied when a climbable step is detected.")]
    public float stepUpForce       = 35f;

    [Header("Stability")]
    [Tooltip("How strongly the bike rights itself upright while grounded.")]
    public float uprightTorque  = 60f;
    [Tooltip("Angular velocity is lerped toward zero by this amount each second.")]
    public float angularDamping = 6f;

    [Header("Traction")]
    [Tooltip("Fraction of sideways velocity cancelled per second. 1 = instant grip, 0 = full slip.")]
    [Range(0f, 1f)]
    public float lateralGrip = 0.92f;
    public float drivingDrag  = 1f;
    public float coastingDrag = 6f;

    [Header("Brake")]
    public float coastBrakeForce = 30f;

    [Header("Multiplayer Motors")]
    public float acceleration = 20f;
    public float brakeForce   = 40f;

    [Header("Wheel Visuals")]
    public float wheelRotationSpeed = 360f;

    [Header("Ground Detection")]
    public float    groundCheckDistance = 0.3f;
    public LayerMask groundLayerMask    = Physics.DefaultRaycastLayers;

    [Header("Airborne")]
    public float extraGravity  = 60f;
    public float selfRightSpeed = 180f;  // deg/s toward upright

    // ── private ──────────────────────────────────────────────────────────────
    private float      leftMotorSpeed;
    private float      rightMotorSpeed;
    private Rigidbody  rb;
    private float      stuckTimer;
    private const float StuckVelThreshold  = 0.25f;
    private const float StuckTimeThreshold = 0.12f;
    private bool        wasGrounded         = true;
    private int         landingGripFrames   = 0;   // extra grip applied right after landing

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.maxAngularVelocity     = 8f;
    }

    private void FixedUpdate()
    {
        bool leftGrounded  = IsWheelGrounded(leftBike_frontWheel)
                          || IsWheelGrounded(leftBike_rearWheel);
        bool rightGrounded = IsWheelGrounded(rightBike_frontWheel)
                          || IsWheelGrounded(rightBike_rearWheel);
        bool grounded      = leftGrounded || rightGrounded;

        // Ground normal for surface-aligned traction (works on ramps too)
        Vector3 groundNormal = GetGroundNormal(grounded);

        // Landing detection — boost lateral grip for a few frames after touching down
        if (grounded && !wasGrounded) landingGripFrames = 8;
        if (landingGripFrames > 0)    landingGripFrames--;
        wasGrounded = grounded;

        if (GameModeManager.IsSingleplayer())
            HandleSingleplayer(grounded, groundNormal);
        else
        {
            HandleMultiplayerInput(leftGrounded, rightGrounded);
            HandleMultiplayer(grounded, groundNormal);
        }

        if (grounded) ApplyStepUp();
        ApplyStability(grounded, groundNormal);

        if (!grounded)
            HandleAirborne();

        HandleWheelRotation();
    }

    // ── singleplayer ──────────────────────────────────────────────────────────
    private void HandleSingleplayer(bool grounded, Vector3 groundNormal)
    {
        float throttle = Input.GetKey(KeyCode.W) ? 1f
                       : Input.GetKey(KeyCode.S) ? -1f : 0f;
        float steer    = Input.GetKey(KeyCode.D) ? 1f
                       : Input.GetKey(KeyCode.A) ? -1f : 0f;

        if (!grounded)
        {
            rb.linearDamping = 0f;
            leftMotorSpeed = rightMotorSpeed = rb.linearVelocity.magnitude * Mathf.Sign(throttle == 0f ? 0f : throttle);
            return;
        }

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // ── Stuck guard ──────────────────────────────────────────────────────
        if (throttle != 0f && rb.linearVelocity.magnitude < StuckVelThreshold)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = 0f;

        bool stuck = stuckTimer >= StuckTimeThreshold;
        if (stuck) rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, StuckVelThreshold);

        // ── Drive force ───────────────────────────────────────────────────────
        if (throttle != 0f && !stuck)
        {
            bool  canDrive  = Mathf.Abs(forwardSpeed) < maxSpeed
                           || Mathf.Sign(forwardSpeed) != Mathf.Sign(throttle);
            float speedRatio = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
            float taper      = 1f - speedRatio;
            float baseForce  = throttle > 0f ? driveForce : reverseForce;

            if (canDrive)
                rb.AddForce(transform.forward * throttle * baseForce * taper,
                            ForceMode.Acceleration);
        }

        // ── Coast brake ───────────────────────────────────────────────────────
        if (throttle == 0f)
            rb.AddForce(-transform.forward * forwardSpeed * coastBrakeForce * Time.fixedDeltaTime,
                        ForceMode.Acceleration);

        // ── Steering: snap yaw angular velocity directly ─────────────────────
        float   targetYaw = steer * maxYawSpeed * Mathf.Deg2Rad;
        Vector3 localAV   = transform.InverseTransformDirection(rb.angularVelocity);
        localAV.y = Mathf.Lerp(localAV.y, targetYaw, steerSnapSpeed * Time.fixedDeltaTime);
        rb.angularVelocity = transform.TransformDirection(localAV);

        // ── Lean ──────────────────────────────────────────────────────────────
        float currentLean = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
        float targetLean  = -steer * maxLeanAngle;
        float leanError   = Mathf.Clamp(targetLean - currentLean, -60f, 60f);
        rb.AddTorque(transform.forward * leanError * leanSnapSpeed * Time.fixedDeltaTime,
                     ForceMode.Acceleration);

        // ── Lateral traction (surface-space) ─────────────────────────────────
        // Project velocity onto the surface plane then cancel the component
        // perpendicular to the bike's forward direction.
        // This works on ramps because we use surface normal, not world up.
        ApplyLateralTraction(groundNormal);

        rb.linearDamping = throttle != 0f ? drivingDrag : coastingDrag;

        leftMotorSpeed = rightMotorSpeed =
            rb.linearVelocity.magnitude * (throttle == 0f ? 0f : Mathf.Sign(throttle));
    }

    // ── multiplayer input ─────────────────────────────────────────────────────
    private void HandleMultiplayerInput(bool leftGrounded, bool rightGrounded)
    {
        bool  braking = Input.GetKey(KeyCode.Space);
        float dt      = Time.fixedDeltaTime;

        if (leftGrounded)
        {
            float t = braking ? 0f : Input.GetKey(KeyCode.W) ? maxSpeed
                    : Input.GetKey(KeyCode.S) ? -maxSpeed : 0f;
            leftMotorSpeed = Mathf.MoveTowards(leftMotorSpeed, t,
                             (braking ? brakeForce : acceleration) * dt);
        }

        if (rightGrounded)
        {
            float t = braking ? 0f : Input.GetKey(KeyCode.UpArrow) ? maxSpeed
                    : Input.GetKey(KeyCode.DownArrow) ? -maxSpeed : 0f;
            rightMotorSpeed = Mathf.MoveTowards(rightMotorSpeed, t,
                              (braking ? brakeForce : acceleration) * dt);
        }
    }

    // ── multiplayer movement ──────────────────────────────────────────────────
    private void HandleMultiplayer(bool grounded, Vector3 groundNormal)
    {
        if (!grounded) { rb.linearDamping = 0f; return; }

        bool throttleHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S)
                         || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);

        float avg  = (leftMotorSpeed + rightMotorSpeed) * 0.5f;
        float diff = leftMotorSpeed - rightMotorSpeed;

        rb.AddForce(transform.forward * avg * driveForce * 0.05f, ForceMode.Acceleration);

        float   targetYaw = diff * maxYawSpeed * Mathf.Deg2Rad * 0.1f;
        Vector3 localAV   = transform.InverseTransformDirection(rb.angularVelocity);
        localAV.y = Mathf.Lerp(localAV.y, targetYaw, steerSnapSpeed * Time.fixedDeltaTime);
        rb.angularVelocity = transform.TransformDirection(localAV);

        ApplyLateralTraction(groundNormal);

        rb.linearDamping = throttleHeld ? drivingDrag : coastingDrag;
    }

    // ── lateral traction ──────────────────────────────────────────────────────
    private void ApplyLateralTraction(Vector3 groundNormal)
    {
        Vector3 sideDir = Vector3.ProjectOnPlane(transform.right, groundNormal).normalized;
        float   lateral = Vector3.Dot(rb.linearVelocity, sideDir);
        // Use full grip (1.0) for landing frames so the bike snaps to track instantly
        float grip = landingGripFrames > 0 ? 1f : lateralGrip;
        rb.linearVelocity -= sideDir * lateral * grip;
    }

    // ── step-up ───────────────────────────────────────────────────────────────
    // Detects low obstacles directly in front of each wheel and applies an
    // upward force so the bike rides over them instead of catching on them.
    private void ApplyStepUp()
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (forwardSpeed < 0.5f) return;  // only when moving forward with some speed

        bool stepDetected = CheckStepAtWheel(leftBike_frontWheel)
                         || CheckStepAtWheel(rightBike_frontWheel);

        if (stepDetected)
            rb.AddForce(Vector3.up * stepUpForce, ForceMode.Acceleration);
    }

    private bool CheckStepAtWheel(Transform wheel)
    {
        if (wheel == null) return false;

        // Cast forward at wheel height to find a wall/ledge face
        Vector3 origin    = wheel.position + Vector3.up * 0.05f;
        Vector3 direction = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        if (!Physics.Raycast(origin, direction, out RaycastHit wallHit,
                             stepCheckDistance, groundLayerMask))
            return false;

        // IMPORTANT: only climb if the wall face is roughly vertical (not a ceiling)
        if (Vector3.Dot(wallHit.normal, Vector3.up) > 0.3f)
            return false;

        // Check if there's climbable ground just above the contact point
        Vector3 aboveHit = wallHit.point + Vector3.up * maxStepHeight;
        if (Physics.Raycast(aboveHit, Vector3.down, out RaycastHit topHit,
                            maxStepHeight * 1.1f, groundLayerMask))
        {
            float stepHeight = topHit.point.y - wheel.position.y;
            // Must be above the wheel (not a pit) and within climbable range
            return stepHeight > 0.02f && stepHeight <= maxStepHeight;
        }

        return false;
    }

    // ── stability (grounded upright torque + angular damping) ─────────────────
    private void ApplyStability(bool grounded, Vector3 groundNormal)
    {
        float dt = Time.fixedDeltaTime;

        // Angular velocity damping — always on, both grounded and airborne.
        // Only damps pitch (X local) and roll (Z local), never yaw (Y local).
        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);
        localAV.x = Mathf.Lerp(localAV.x, 0f, angularDamping * dt);
        localAV.z = Mathf.Lerp(localAV.z, 0f, angularDamping * dt);
        rb.angularVelocity = transform.TransformDirection(localAV);

        if (!grounded) return;

        // Upright torque: push the bike's up axis toward the ground normal.
        // Scale down at high speed — at speed the bike is stable and the torque
        // was causing it to dig into ledges instead of riding over them.
        float speedFrac    = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
        float torqueScale  = Mathf.Lerp(1f, 0.15f, speedFrac);
        Vector3 torqueAxis = Vector3.Cross(transform.up, groundNormal);
        rb.AddTorque(torqueAxis * uprightTorque * torqueScale, ForceMode.Acceleration);
    }

    // ── airborne ──────────────────────────────────────────────────────────────
    private void HandleAirborne()
    {
        rb.linearDamping = 0f;
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);

        // Self-right toward world upright using MoveRotation — only pitch/roll,
        // preserves yaw. Gated on angular velocity being below max so it never
        // fights a spinning rigidbody.
        if (rb.angularVelocity.magnitude < 6f)
        {
            Vector3    fwd    = transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            Quaternion target = Quaternion.LookRotation(fwd.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target,
                            selfRightSpeed * Time.fixedDeltaTime));
        }
    }

    // ── ground normal ─────────────────────────────────────────────────────────
    private Vector3 GetGroundNormal(bool grounded)
    {
        if (!grounded) return Vector3.up;

        // Average the normals from all four wheel positions for a stable result
        Vector3 sum   = Vector3.zero;
        int     count = 0;
        TryGetNormal(leftBike_frontWheel,  ref sum, ref count);
        TryGetNormal(leftBike_rearWheel,   ref sum, ref count);
        TryGetNormal(rightBike_frontWheel, ref sum, ref count);
        TryGetNormal(rightBike_rearWheel,  ref sum, ref count);

        return count > 0 ? (sum / count).normalized : Vector3.up;
    }

    private void TryGetNormal(Transform wheel, ref Vector3 sum, ref int count)
    {
        if (wheel == null) return;
        if (Physics.Raycast(wheel.position, Vector3.down, out RaycastHit hit,
                            groundCheckDistance, groundLayerMask))
        {
            sum += hit.normal;
            count++;
        }
    }

    // ── wheel visuals ─────────────────────────────────────────────────────────
    private void HandleWheelRotation()
    {
        float dt = Time.fixedDeltaTime;

        if (GameModeManager.IsSingleplayer())
        {
            float spin = rb.linearVelocity.magnitude * 0.5f;
            SpinWheel(leftBike_frontWheel,  spin, dt);
            SpinWheel(leftBike_rearWheel,   spin, dt);
            SpinWheel(rightBike_frontWheel, spin, dt);
            SpinWheel(rightBike_rearWheel,  spin, dt);
        }
        else
        {
            SpinWheel(leftBike_frontWheel,  leftMotorSpeed,  dt);
            SpinWheel(leftBike_rearWheel,   leftMotorSpeed,  dt);
            SpinWheel(rightBike_frontWheel, rightMotorSpeed, dt);
            SpinWheel(rightBike_rearWheel,  rightMotorSpeed, dt);
        }
    }

    private void SpinWheel(Transform wheel, float speed, float dt)
    {
        if (wheel == null) return;
        wheel.Rotate(Vector3.right * speed * wheelRotationSpeed * dt, Space.Self);
    }

    // ── ground detection ──────────────────────────────────────────────────────
    private bool IsWheelGrounded(Transform wheel)
    {
        if (wheel == null) return false;
        return Physics.Raycast(wheel.position, Vector3.down,
                               groundCheckDistance, groundLayerMask);
    }

    // ── public API ────────────────────────────────────────────────────────────
    public void ResetSpeeds()
    {
        leftMotorSpeed = 0f; rightMotorSpeed = 0f; stuckTimer = 0f;
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}