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
    public float     groundCheckDistance = 0.3f;
    public LayerMask groundLayerMask     = Physics.DefaultRaycastLayers;

    [Header("Airborne")]
    public float extraGravity   = 60f;
    public float selfRightSpeed = 180f;  // deg/s toward upright

    // ── private ───────────────────────────────────────────────────────────────
    private float       leftMotorSpeed;
    private float       rightMotorSpeed;
    private Rigidbody   rb;
    private bool        wasGrounded       = true;
    private int         landingGripFrames = 0;

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

        Vector3 groundNormal = GetGroundNormal(grounded);

        // Landing detection — boost lateral grip for a few frames after touching down
        if (grounded && !wasGrounded) landingGripFrames = 8;
        if (landingGripFrames > 0)    landingGripFrames--;
        wasGrounded = grounded;

        // Both modes now share the same motor/movement pipeline.
        // Input gathering is split so WASD vs arrow keys can be configured per-side.
        if (GameModeManager.IsSingleplayer())
            HandleSingleplayerInput(leftGrounded, rightGrounded);
        else
            HandleMultiplayerInput(leftGrounded, rightGrounded);

        HandleDifferentialDrive(grounded, groundNormal);

        if (grounded) ApplyStepUp();
        ApplyStability(grounded, groundNormal);

        if (!grounded) HandleAirborne();

        HandleWheelRotation();
    }

    // ── singleplayer input (WASD, differential-drive mapping) ─────────────────
    // W/S = both motors, A/D = oppose the motors to yaw.
    // Braking (Space) zeroes both targets and ramps down fast.
    private void HandleSingleplayerInput(bool leftGrounded, bool rightGrounded)
    {
        bool  braking  = Input.GetKey(KeyCode.Space);
        float throttle = Input.GetKey(KeyCode.W) ? 1f
                       : Input.GetKey(KeyCode.S) ? -1f : 0f;
        float steer    = Input.GetKey(KeyCode.D) ? 1f
                       : Input.GetKey(KeyCode.A) ? -1f : 0f;

        // Tank-drive mixing: steer subtracts from one side, adds to the other.
        // Using maxSpeed as the target so the ramp behaviour is identical to multiplayer.
        float leftTarget  = braking ? 0f : (throttle + steer) * maxSpeed;
        float rightTarget = braking ? 0f : (throttle - steer) * maxSpeed;
        leftTarget  = Mathf.Clamp(leftTarget,  -maxSpeed, maxSpeed);
        rightTarget = Mathf.Clamp(rightTarget, -maxSpeed, maxSpeed);

        float rate = braking ? brakeForce : acceleration;
        float dt   = Time.fixedDeltaTime;

        if (leftGrounded)
            leftMotorSpeed  = Mathf.MoveTowards(leftMotorSpeed,  leftTarget,  rate * dt);
        if (rightGrounded)
            rightMotorSpeed = Mathf.MoveTowards(rightMotorSpeed, rightTarget, rate * dt);
    }

    // ── multiplayer input (unchanged) ─────────────────────────────────────────
    private void HandleMultiplayerInput(bool leftGrounded, bool rightGrounded)
    {
        bool  braking = Input.GetKey(KeyCode.Space);
        float dt      = Time.fixedDeltaTime;

        if (leftGrounded)
        {
            float t = braking ? 0f : Input.GetKey(KeyCode.W)         ? maxSpeed
                    : Input.GetKey(KeyCode.S)         ? -maxSpeed : 0f;
            leftMotorSpeed = Mathf.MoveTowards(leftMotorSpeed, t,
                             (braking ? brakeForce : acceleration) * dt);
        }

        if (rightGrounded)
        {
            float t = braking ? 0f : Input.GetKey(KeyCode.UpArrow)   ? maxSpeed
                    : Input.GetKey(KeyCode.DownArrow) ? -maxSpeed : 0f;
            rightMotorSpeed = Mathf.MoveTowards(rightMotorSpeed, t,
                              (braking ? brakeForce : acceleration) * dt);
        }
    }

    // ── shared differential-drive movement ────────────────────────────────────
    // Called for BOTH singleplayer and multiplayer — one movement path.
    private void HandleDifferentialDrive(bool grounded, Vector3 groundNormal)
    {
        if (!grounded) { rb.linearDamping = 0f; return; }

        bool throttleHeld = leftMotorSpeed != 0f || rightMotorSpeed != 0f;

        float avg  = (leftMotorSpeed + rightMotorSpeed) * 0.5f;
        float diff = leftMotorSpeed - rightMotorSpeed;

        // Drive
        rb.AddForce(transform.forward * avg * driveForce * 0.05f, ForceMode.Acceleration);

        // Yaw from differential
        float   targetYaw = diff * maxYawSpeed * Mathf.Deg2Rad * 0.1f;
        Vector3 localAV   = transform.InverseTransformDirection(rb.angularVelocity);
        localAV.y = Mathf.Lerp(localAV.y, targetYaw, steerSnapSpeed * Time.fixedDeltaTime);
        rb.angularVelocity = transform.TransformDirection(localAV);

        // Lean proportional to yaw rate
        float steerInput   = Mathf.Clamp(diff / (maxSpeed * 2f), -1f, 1f);
        float currentLean  = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
        float targetLean   = -steerInput * maxLeanAngle;
        float leanError    = Mathf.Clamp(targetLean - currentLean, -60f, 60f);
        rb.AddTorque(transform.forward * leanError * leanSnapSpeed * Time.fixedDeltaTime,
                     ForceMode.Acceleration);

        // Lateral traction
        ApplyLateralTraction(groundNormal);

        rb.linearDamping = throttleHeld ? drivingDrag : coastingDrag;
    }

    // ── lateral traction ──────────────────────────────────────────────────────
    private void ApplyLateralTraction(Vector3 groundNormal)
    {
        Vector3 sideDir = Vector3.ProjectOnPlane(transform.right, groundNormal).normalized;
        float   lateral = Vector3.Dot(rb.linearVelocity, sideDir);
        float   grip    = landingGripFrames > 0 ? 1f : lateralGrip;
        rb.linearVelocity -= sideDir * lateral * grip;
    }

    // ── step-up ───────────────────────────────────────────────────────────────
    private void ApplyStepUp()
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (forwardSpeed < 0.5f) return;

        bool stepDetected = CheckStepAtWheel(leftBike_frontWheel)
                         || CheckStepAtWheel(rightBike_frontWheel);

        if (stepDetected)
            rb.AddForce(Vector3.up * stepUpForce, ForceMode.Acceleration);
    }

    private bool CheckStepAtWheel(Transform wheel)
    {
        if (wheel == null) return false;

        Vector3 origin    = wheel.position + Vector3.up * 0.05f;
        Vector3 direction = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        if (!Physics.Raycast(origin, direction, out RaycastHit wallHit,
                             stepCheckDistance, groundLayerMask))
            return false;

        if (Vector3.Dot(wallHit.normal, Vector3.up) > 0.3f)
            return false;

        Vector3 aboveHit = wallHit.point + Vector3.up * maxStepHeight;
        if (Physics.Raycast(aboveHit, Vector3.down, out RaycastHit topHit,
                            maxStepHeight * 1.1f, groundLayerMask))
        {
            float stepHeight = topHit.point.y - wheel.position.y;
            return stepHeight > 0.02f && stepHeight <= maxStepHeight;
        }

        return false;
    }

    // ── stability ─────────────────────────────────────────────────────────────
    private void ApplyStability(bool grounded, Vector3 groundNormal)
    {
        float   dt      = Time.fixedDeltaTime;
        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);
        localAV.x = Mathf.Lerp(localAV.x, 0f, angularDamping * dt);
        localAV.z = Mathf.Lerp(localAV.z, 0f, angularDamping * dt);
        rb.angularVelocity = transform.TransformDirection(localAV);

        if (!grounded) return;

        float   speedFrac   = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
        float   torqueScale = Mathf.Lerp(1f, 0.15f, speedFrac);
        Vector3 torqueAxis  = Vector3.Cross(transform.up, groundNormal);
        rb.AddTorque(torqueAxis * uprightTorque * torqueScale, ForceMode.Acceleration);
    }

    // ── airborne ──────────────────────────────────────────────────────────────
    private void HandleAirborne()
    {
        rb.linearDamping = 0f;
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);

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
        SpinWheel(leftBike_frontWheel,  leftMotorSpeed,  dt);
        SpinWheel(leftBike_rearWheel,   leftMotorSpeed,  dt);
        SpinWheel(rightBike_frontWheel, rightMotorSpeed, dt);
        SpinWheel(rightBike_rearWheel,  rightMotorSpeed, dt);
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
        leftMotorSpeed = 0f; rightMotorSpeed = 0f;
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}