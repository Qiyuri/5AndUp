using UnityEngine;

public class TestMovement : MonoBehaviour
{
    [Header("Left Bike Wheels")]
    public Transform leftBike_frontWheel;
    public Transform leftBike_rearWheel;

    [Header("Right Bike Wheels")]
    public Transform rightBike_frontWheel;
    public Transform rightBike_rearWheel;

    [Header("Hamsteria Physics")]
    public float driveForce     = 70f;
    public float reverseForce   = 50f;
    public float steeringTorque = 8f;
    public float leanTorque     = 16f;
    public float balanceTorque  = 24f;
    [Tooltip("How strongly the bike resists pitching forward/backward while grounded.")]
    public float pitchTorque     = 20f;
    public float maxLeanAngle   = 35f;

    [Header("Traction")]
    [Tooltip("How hard lateral (sideways) slip is cancelled each frame. Higher = less drift.")]
    public float lateralFriction = 15f;
    [Tooltip("Linear drag applied while grounded and throttle is held.")]
    public float drivingDrag = 1f;
    [Tooltip("Linear drag applied while grounded and coasting (no throttle).")]
    public float coastingDrag = 8f;

    [Header("Steering")]
    [Tooltip("How quickly yaw velocity reaches the target. Higher = snappier turning.")]
    public float steerSnapSpeed = 12f;
    [Tooltip("Max yaw rotation speed in degrees/sec.")]
    public float maxYawSpeed = 90f;

    [Header("Stability")]
    public float maxAngularVelocity    = 4f;
    public float angularDampingGrounded = 8f;
    public float angularDampingAirborne = 5f;
    public float maxCorrectionAngle    = 45f;

    [Header("Motor Settings")]
    public float maxSpeed     = 5f;
    public float acceleration = 20f;
    public float brakeForce   = 40f;

    [Header("Wheel Visuals")]
    public float wheelRotationSpeed = 360f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayerMask = Physics.DefaultRaycastLayers;
    [Tooltip("If the ground normal tilts more than this many degrees from vertical, disable lateral friction and linear damping so the bike flows freely with the ramp.")]
    public float maxFlatAngle = 15f;

    [Header("Airborne")]
    public float extraGravityForce = 20f;
    public float selfRightSpeed    = 90f;

    // ── private state ────────────────────────────────────────────────────────
    private float leftMotorSpeed;
    private float rightMotorSpeed;
    private Rigidbody rb;

    // ── lifecycle ────────────────────────────────────────────────────────────
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.maxAngularVelocity     = maxAngularVelocity;
    }

    private void FixedUpdate()
    {
        bool leftGrounded  = IsWheelGrounded(leftBike_frontWheel)
                          || IsWheelGrounded(leftBike_rearWheel);
        bool rightGrounded = IsWheelGrounded(rightBike_frontWheel)
                          || IsWheelGrounded(rightBike_rearWheel);
        bool grounded      = leftGrounded || rightGrounded;

        if (GameModeManager.IsSingleplayer())
            HandleSingleplayerMovement(grounded);
        else
        {
            HandleMultiplayerInput(leftGrounded, rightGrounded);
            HandleMultiplayerMovement(grounded);
        }

        // Only apply lateral friction and linear damping on flat ground.
        // On ramps the bike should flow freely with the surface.
        bool onFlatGround = grounded && IsGroundFlat();
        if (onFlatGround)
            ApplyLateralFriction();

        ApplyAngularDamping(grounded);
        HandleAirborne(grounded);
        HandleWheelRotation();
    }

    // ── lateral friction ──────────────────────────────────────────────────────
    // Cancels sideways velocity directly — eliminates drift/sliding without
    // affecting forward or vertical movement at all.
    private void ApplyLateralFriction()
    {
        Vector3 localVel    = transform.InverseTransformDirection(rb.linearVelocity);
        float   lateralSlip = localVel.x;                         // X = right in local space
        float   correction  = -lateralSlip * lateralFriction * Time.fixedDeltaTime;
        correction          = Mathf.Clamp(correction, -Mathf.Abs(lateralSlip), Mathf.Abs(lateralSlip));
        rb.linearVelocity  += transform.right * correction;
    }

    // ── angular damping ───────────────────────────────────────────────────────
    private void ApplyAngularDamping(bool grounded)
    {
        float dt = Time.fixedDeltaTime;

        if (!grounded)
        {
            rb.angularVelocity = Vector3.Lerp(
                rb.angularVelocity, Vector3.zero,
                angularDampingAirborne * dt);
            return;
        }

        // Grounded: damp pitch and roll in local space, leave yaw free.
        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);
        localAV.x = Mathf.Lerp(localAV.x, 0f, angularDampingGrounded * dt);
        localAV.z = Mathf.Lerp(localAV.z, 0f, angularDampingGrounded * dt);
        rb.angularVelocity = transform.TransformDirection(localAV);
    }

    // ── singleplayer ─────────────────────────────────────────────────────────
    private void HandleSingleplayerMovement(bool grounded)
    {
        float throttle = 0f;
        float steer    = 0f;

        if (Input.GetKey(KeyCode.W))      throttle =  1f;
        else if (Input.GetKey(KeyCode.S)) throttle = -1f;

        if (Input.GetKey(KeyCode.A))      steer = -1f;
        else if (Input.GetKey(KeyCode.D)) steer =  1f;

        if (grounded)
        {
            // ── Drive force — tapered acceleration ───────────────────────────
            // Force is full at standstill and tapers to zero at maxSpeed,
            // giving a snappy start that smoothly levels off at top speed.
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            bool  belowMax     = Mathf.Abs(forwardSpeed) < maxSpeed;

            if (throttle != 0f && (belowMax || Mathf.Sign(forwardSpeed) != Mathf.Sign(throttle)))
            {
                float speedRatio  = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
                float taper       = 1f - speedRatio;               // 1 at 0 speed → 0 at maxSpeed
                float baseForce   = throttle > 0f ? driveForce : reverseForce;
                float force       = throttle * baseForce * taper;
                rb.AddForce(transform.forward * force, ForceMode.Acceleration);
            }

            // ── Active coast braking ─────────────────────────────────────────
            // When no throttle is held, push back against the current forward
            // velocity so the bike stops quickly instead of rolling far.
            if (throttle == 0f && IsGroundFlat())
            {
                float brakePush = -forwardSpeed * coastingDrag * 0.5f;
                rb.AddForce(transform.forward * brakePush, ForceMode.Acceleration);
            }

            // ── Steering: direct yaw velocity snap, no torque buildup ────────
            // Instead of accumulating torque over frames, we directly drive
            // the world-space yaw component of angularVelocity toward a target.
            // This gives frame-1 response with no delay.
            float targetYawSpeed  = steer * maxYawSpeed * Mathf.Deg2Rad;
            Vector3 localAV       = transform.InverseTransformDirection(rb.angularVelocity);
            localAV.y             = Mathf.Lerp(localAV.y, targetYawSpeed,
                                               steerSnapSpeed * Time.fixedDeltaTime);
            rb.angularVelocity    = transform.TransformDirection(localAV);

            // ── Lean into turns ──────────────────────────────────────────────
            float currentLean = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
            float targetLean  = -steer * maxLeanAngle;
            float leanError   = Mathf.Clamp(targetLean - currentLean,
                                            -maxCorrectionAngle, maxCorrectionAngle);

            rb.AddTorque(transform.forward * leanError * leanTorque * Time.fixedDeltaTime,
                         ForceMode.Acceleration);

            // ── Self-balance (roll) ──────────────────────────────────────────
            float rollError = Mathf.Clamp(
                Vector3.SignedAngle(transform.up, Vector3.up, transform.forward),
                -maxCorrectionAngle, maxCorrectionAngle);

            rb.AddTorque(transform.forward * rollError * balanceTorque * Time.fixedDeltaTime,
                         ForceMode.Acceleration);

            // ── Pitch correction ─────────────────────────────────────────────
            // Resists the bike tipping forward/backward when accelerating or
            // braking. Measures how far transform.forward has tilted from flat.
            float pitchError = Mathf.Clamp(
                Vector3.SignedAngle(
                    new Vector3(transform.forward.x, 0f, transform.forward.z).normalized,
                    transform.forward,
                    transform.right),
                -maxCorrectionAngle, maxCorrectionAngle);

            rb.AddTorque(-transform.right * pitchError * pitchTorque * Time.fixedDeltaTime,
                         ForceMode.Acceleration);

            // ── Drag (flat ground only) ──────────────────────────────────────
            // On ramps: zero damping so the bike rides the surface naturally.
            if (IsGroundFlat())
                rb.linearDamping = throttle != 0f ? drivingDrag : coastingDrag;
            else
                rb.linearDamping = 0f;
        }
        else
        {
            rb.linearDamping = 0f;
        }

        // Wheel visuals
        float sign = throttle == 0f ? 0f : Mathf.Sign(throttle);
        leftMotorSpeed  = rb.linearVelocity.magnitude * sign;
        rightMotorSpeed = leftMotorSpeed;
    }

    // ── multiplayer input ────────────────────────────────────────────────────
    private void HandleMultiplayerInput(bool leftGrounded, bool rightGrounded)
    {
        bool  braking = Input.GetKey(KeyCode.Space);
        float dt      = Time.fixedDeltaTime;

        if (leftGrounded)
        {
            float target = 0f;
            if (!braking)
            {
                if (Input.GetKey(KeyCode.W))      target =  maxSpeed;
                else if (Input.GetKey(KeyCode.S)) target = -maxSpeed;
            }
            leftMotorSpeed = Mathf.MoveTowards(leftMotorSpeed, target,
                             (braking ? brakeForce : acceleration) * dt);
        }

        if (rightGrounded)
        {
            float target = 0f;
            if (!braking)
            {
                if (Input.GetKey(KeyCode.UpArrow))        target =  maxSpeed;
                else if (Input.GetKey(KeyCode.DownArrow)) target = -maxSpeed;
            }
            rightMotorSpeed = Mathf.MoveTowards(rightMotorSpeed, target,
                              (braking ? brakeForce : acceleration) * dt);
        }
    }

    // ── multiplayer movement ─────────────────────────────────────────────────
    private void HandleMultiplayerMovement(bool grounded)
    {
        bool throttleHeld = Input.GetKey(KeyCode.W)       || Input.GetKey(KeyCode.S)
                         || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);

        if (!grounded) { rb.linearDamping = 0f; return; }

        float averageSpeed = (leftMotorSpeed + rightMotorSpeed) * 0.5f;
        float speedDiff    = leftMotorSpeed - rightMotorSpeed;

        rb.AddForce(transform.forward * averageSpeed * driveForce * 0.05f,
                    ForceMode.Acceleration);

        // Differential steering via direct yaw snap as well
        float targetYawSpeed = speedDiff * maxYawSpeed * Mathf.Deg2Rad * 0.1f;
        Vector3 localAV      = transform.InverseTransformDirection(rb.angularVelocity);
        localAV.y            = Mathf.Lerp(localAV.y, targetYawSpeed,
                                          steerSnapSpeed * Time.fixedDeltaTime);
        rb.angularVelocity   = transform.TransformDirection(localAV);

        float rollError = Mathf.Clamp(
            Vector3.SignedAngle(transform.up, Vector3.up, transform.forward),
            -maxCorrectionAngle, maxCorrectionAngle);

        rb.AddTorque(transform.forward * rollError * balanceTorque * Time.fixedDeltaTime,
                     ForceMode.Acceleration);

        rb.linearDamping = IsGroundFlat()
            ? (throttleHeld ? drivingDrag : coastingDrag)
            : 0f;
    }

    // ── airborne ─────────────────────────────────────────────────────────────
    private void HandleAirborne(bool grounded)
    {
        if (grounded) return;

        rb.AddForce(Vector3.down * extraGravityForce, ForceMode.Acceleration);

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;

        Quaternion uprightTarget = Quaternion.LookRotation(fwd.normalized, Vector3.up);

        if (rb.angularVelocity.magnitude < maxAngularVelocity * 0.8f)
        {
            rb.MoveRotation(Quaternion.RotateTowards(
                rb.rotation, uprightTarget,
                selfRightSpeed * Time.fixedDeltaTime));
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
    // Returns true when the surface directly below the bike is close to flat.
    private bool IsGroundFlat()
    {
        // Cast from the centre of the bike downward and check the surface normal.
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                            groundCheckDistance + 0.5f, groundLayerMask))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            return angle <= maxFlatAngle;
        }
        return true; // no hit = treat as flat (airborne path handles it anyway)
    }

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