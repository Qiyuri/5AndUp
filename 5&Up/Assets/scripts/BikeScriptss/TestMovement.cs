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
    public float maxYawSpeed    = 120f;
    public float steerSnapSpeed = 14f;
    public float maxLeanAngle   = 30f;
    public float leanSnapSpeed  = 10f;

    [Header("Step-Up")]
    public float stepCheckDistance = 0.5f;
    public float maxStepHeight     = 0.4f;
    public float stepUpForce       = 35f;

    [Header("Stability")]
    public float uprightTorque  = 60f;
    public float angularDamping = 6f;

    [Header("Traction")]
    [Range(0f, 1f)]
    public float lateralGrip  = 0.92f;
    public float drivingDrag  = 0.2f;
    public float coastingDrag = 6f;

    [Header("Brake")]
    public float coastBrakeForce = 30f;

    [Header("Motors")]
    public float acceleration = 80f;
    public float brakeForce   = 40f;

    [Header("Wheel Visuals")]
    public float wheelRotationSpeed = 360f;

    [Header("Ground Detection")]
    public float     groundCheckDistance = 0.3f;
    public LayerMask groundLayerMask     = Physics.DefaultRaycastLayers;

    [Header("Airborne")]
    public float extraGravity   = 60f;
    [Tooltip("Hoe snel de fiets rechtop draait in de lucht. Lager = zachter.")]
    public float selfRightSpeed = 45f;
    [Tooltip("Hoeveel frames de fiets in de lucht moet zijn voor het zichzelf rechtop draait. Voorkomt snappen bij botsingen.")]
    public int   airborneFramesBeforeSelfRight = 8;

    // ── private ───────────────────────────────────────────────────────────────
    private float     leftMotorSpeed;
    private float     rightMotorSpeed;
    private Rigidbody rb;
    private bool      wasGrounded       = true;
    private int       landingGripFrames = 0;
    private int       airborneFrames    = 0;

    [Header("Singleplayer Steering")]
    [Tooltip("How fast steering builds up at low/zero speed. Lower = smoother, higher = snappier.")]
    public float steerSmoothSpeed = 4f;
    [Tooltip("At max speed, steerSmoothSpeed is multiplied by this. 0.2 = 20% → very slow steer buildup at high speed.")]
    public float highSpeedSteerMultiplier = 0.25f;

    // Raw singleplayer steer input (-1/0/1 from key press)
    private float _spRawSteer    = 0f;
    // Smoothed version used for the actual yaw drive
    private float _smoothedSteer = 0f;

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

        // ── Landing ───────────────────────────────────────────────────────────
        if (grounded && !wasGrounded)
        {
            landingGripFrames = 8;
            airborneFrames    = 0;

            float actualForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            leftMotorSpeed  = Mathf.Clamp(leftMotorSpeed,
                                          -Mathf.Abs(actualForwardSpeed),
                                           Mathf.Abs(actualForwardSpeed));
            rightMotorSpeed = Mathf.Clamp(rightMotorSpeed,
                                          -Mathf.Abs(actualForwardSpeed),
                                           Mathf.Abs(actualForwardSpeed));
        }

        if (!grounded) airborneFrames++;
        else           airborneFrames = 0;

        if (landingGripFrames > 0) landingGripFrames--;

        wasGrounded = grounded;

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

    // ── singleplayer input ────────────────────────────────────────────────────
    private void HandleSingleplayerInput(bool leftGrounded, bool rightGrounded)
    {
        bool  braking  = Input.GetKey(KeyCode.Space);
        float throttle = Input.GetKey(KeyCode.W) ? 1f
                       : Input.GetKey(KeyCode.S) ? -1f : 0f;

        // Store raw steer so HandleDifferentialDrive can use it directly for yaw.
        _spRawSteer = Input.GetKey(KeyCode.D) ?  1f
                    : Input.GetKey(KeyCode.A) ? -1f : 0f;

        // Motor targets — steer still mixed in so wheels spin correctly
        // and forward movement steers naturally.
        float leftTarget  = braking ? 0f : Mathf.Clamp((throttle + _spRawSteer) * maxSpeed, -maxSpeed, maxSpeed);
        float rightTarget = braking ? 0f : Mathf.Clamp((throttle - _spRawSteer) * maxSpeed, -maxSpeed, maxSpeed);

        float rate = braking ? brakeForce : acceleration;
        float dt   = Time.fixedDeltaTime;

        if (leftGrounded)
            leftMotorSpeed  = Mathf.MoveTowards(leftMotorSpeed,  leftTarget,  rate * dt);
        if (rightGrounded)
            rightMotorSpeed = Mathf.MoveTowards(rightMotorSpeed, rightTarget, rate * dt);
    }

    // ── multiplayer input ─────────────────────────────────────────────────────
    private void HandleMultiplayerInput(bool leftGrounded, bool rightGrounded)
    {
        bool  braking = Input.GetKey(KeyCode.Space);
        float dt      = Time.fixedDeltaTime;

        if (leftGrounded)
        {
            float t = braking ? 0f : Input.GetKey(KeyCode.W) ?  maxSpeed
                    :               Input.GetKey(KeyCode.S)   ? -maxSpeed : 0f;
            leftMotorSpeed = Mathf.MoveTowards(leftMotorSpeed, t,
                             (braking ? brakeForce : acceleration) * dt);
        }

        if (rightGrounded)
        {
            float t = braking ? 0f : Input.GetKey(KeyCode.UpArrow)   ?  maxSpeed
                    :               Input.GetKey(KeyCode.DownArrow)   ? -maxSpeed : 0f;
            rightMotorSpeed = Mathf.MoveTowards(rightMotorSpeed, t,
                              (braking ? brakeForce : acceleration) * dt);
        }

        _spRawSteer = 0f; // unused in multiplayer
    }

    // ── shared differential-drive movement ────────────────────────────────────
    private void HandleDifferentialDrive(bool grounded, Vector3 groundNormal)
    {
        if (!grounded) { rb.linearDamping = 0f; return; }

        bool throttleHeld = leftMotorSpeed != 0f || rightMotorSpeed != 0f;

        float avg = (leftMotorSpeed + rightMotorSpeed) * 0.5f;
        rb.AddForce(transform.forward * avg * driveForce * 0.15f, ForceMode.Acceleration);

        // ── Yaw ───────────────────────────────────────────────────────────────
        // Singleplayer: smooth the raw steer toward the input so turning
        // builds up gradually. But snap damping uses the RAW input — so
        // the moment the key is released, yaw stops hard (no drift).
        // Multiplayer: unchanged, uses motor differential.
        if (GameModeManager.IsSingleplayer())
        {
            // At low speed: full steerSmoothSpeed. At maxSpeed: multiplied by
            // highSpeedSteerMultiplier — so steering builds up much slower at pace.
            float forwardSpeed   = Mathf.Abs(Vector3.Dot(rb.linearVelocity, transform.forward));
            float speedRatio     = Mathf.Clamp01(forwardSpeed / maxSpeed);
            float effectiveSmooth = Mathf.Lerp(steerSmoothSpeed,
                                               steerSmoothSpeed * highSpeedSteerMultiplier,
                                               speedRatio);
            _smoothedSteer = Mathf.MoveTowards(_smoothedSteer, _spRawSteer,
                                               effectiveSmooth * Time.fixedDeltaTime);
        }

        float diff = GameModeManager.IsSingleplayer()
            ? _smoothedSteer * maxSpeed * 2f
            : leftMotorSpeed - rightMotorSpeed;

        float   targetYaw = diff * maxYawSpeed * Mathf.Deg2Rad * 0.1f;
        Vector3 localAV   = transform.InverseTransformDirection(rb.angularVelocity);

        // Key released in singleplayer → snap yaw to zero immediately.
        // Key held → let MoveTowards smoothly follow targetYaw.
        bool steerReleased = GameModeManager.IsSingleplayer()
            ? Mathf.Abs(_spRawSteer) < 0.01f
            : Mathf.Abs(diff) < 0.01f;

        float yawDampRate = steerReleased ? steerSnapSpeed * 6f : steerSnapSpeed;

        localAV.y = Mathf.MoveTowards(localAV.y, targetYaw, yawDampRate * Time.fixedDeltaTime);
        rb.angularVelocity = transform.TransformDirection(localAV);

        // ── Lean ──────────────────────────────────────────────────────────────
        float steerInput  = Mathf.Clamp(diff / (maxSpeed * 2f), -1f, 1f);
        float currentLean = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
        float targetLean  = -steerInput * maxLeanAngle;
        float leanError   = Mathf.Clamp(targetLean - currentLean, -60f, 60f);
        rb.AddTorque(transform.forward * leanError * leanSnapSpeed * Time.fixedDeltaTime,
                     ForceMode.Acceleration);

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

        Vector3 bikeUp    = transform.up;
        Vector3 rightAxis = transform.forward;

        float tiltAngle = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(bikeUp,       rightAxis).normalized,
            Vector3.ProjectOnPlane(groundNormal, rightAxis).normalized,
            rightAxis
        );

        float torqueMagnitude = tiltAngle * uprightTorque * 0.05f;
        rb.AddTorque(rightAxis * torqueMagnitude, ForceMode.Acceleration);
    }

    // ── airborne ──────────────────────────────────────────────────────────────
    private void HandleAirborne()
    {
        rb.linearDamping = 0f;
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);

        bool settledEnough   = rb.angularVelocity.magnitude < 6f;
        bool longEnoughInAir = airborneFrames >= airborneFramesBeforeSelfRight;

        if (settledEnough && longEnoughInAir)
        {
            Vector3 fwd = transform.forward; fwd.y = 0f;
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
        leftMotorSpeed  = 0f;
        rightMotorSpeed = 0f;
        _spRawSteer     = 0f;
        _smoothedSteer  = 0f;
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}