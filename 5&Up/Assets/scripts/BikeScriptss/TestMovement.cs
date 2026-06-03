using UnityEngine;

/// <summary>
/// Arcade bike controller — Hamsteria-style feel.
///
/// Left motor  : W / S keys
/// Right motor : Up / Down Arrow keys
/// Brake       : Space
/// </summary>
public class TestMovement : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Left Bike Transforms")]
    public Transform leftBike_frontWheel;
    public Transform leftBike_rearWheel;

    [Header("Right Bike Transforms")]
    public Transform rightBike_frontWheel;
    public Transform rightBike_rearWheel;

    [Header("Movement")]
    public float maxSpeed     = 5f;
    public float acceleration = 20f;
    public float brakeForce   = 40f;

    [Header("Turning")]
    public float turnSpeed = 120f;

    [Header("Wheel Visuals")]
    public float wheelRotationSpeed = 360f;

    [Header("Ground Detection")]
    [Tooltip("Ray length for a wheel to be considered grounded.")]
    public float groundCheckDistance = 0.2f;
    [Tooltip("Assign to your ground layer to exclude the bike's own colliders.")]
    public LayerMask groundLayerMask = Physics.DefaultRaycastLayers;

    [Header("Airborne — Extra Gravity")]
    [Tooltip("Extra downward force added on top of Unity gravity while airborne.")]
    public float extraGravityForce = 20f;

    [Header("Airborne — Self-Righting")]
    [Tooltip("Degrees per second the bike rotates back to upright while airborne.")]
    public float selfRightSpeed = 90f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private float leftMotorSpeed;
    private float rightMotorSpeed;
    private Rigidbody rb;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        bool leftGrounded  = IsAnyWheelGrounded(leftBike_frontWheel,  leftBike_rearWheel);
        bool rightGrounded = IsAnyWheelGrounded(rightBike_frontWheel, rightBike_rearWheel);
        bool anyGrounded   = leftGrounded || rightGrounded;

        HandleInput(leftGrounded, rightGrounded);
        HandleMovement(anyGrounded);
        HandleAirborne(anyGrounded);
        HandleWheelRotation();
    }

    // -------------------------------------------------------------------------
    // Input — speed only changes while grounded; airborne preserves momentum
    // -------------------------------------------------------------------------

    private void HandleInput(bool leftGrounded, bool rightGrounded)
    {
        bool  braking = Input.GetKey(KeyCode.Space);
        float dt      = Time.fixedDeltaTime;

        if (leftGrounded)
        {
            float target = 0f;
            if (!braking)
            {
                if      (Input.GetKey(KeyCode.W)) target =  maxSpeed;
                else if (Input.GetKey(KeyCode.S)) target = -maxSpeed;
            }
            leftMotorSpeed = Mathf.MoveTowards(
                leftMotorSpeed, target,
                (braking ? brakeForce : acceleration) * dt);
        }

        if (rightGrounded)
        {
            float target = 0f;
            if (!braking)
            {
                if      (Input.GetKey(KeyCode.UpArrow))   target =  maxSpeed;
                else if (Input.GetKey(KeyCode.DownArrow)) target = -maxSpeed;
            }
            rightMotorSpeed = Mathf.MoveTowards(
                rightMotorSpeed, target,
                (braking ? brakeForce : acceleration) * dt);
        }
    }

    // -------------------------------------------------------------------------
    // Movement — tank-steer; snap to rest when grounded and coasting
    // -------------------------------------------------------------------------

    private void HandleMovement(bool anyGrounded)
    {
        float dt           = Time.fixedDeltaTime;
        float averageSpeed = (leftMotorSpeed + rightMotorSpeed) * 0.5f;
        float speedDiff    = leftMotorSpeed - rightMotorSpeed;

        // Snap residual creep to zero while grounded so the bike feels planted.
        if (anyGrounded && Mathf.Abs(averageSpeed) < 0.05f)
        {
            leftMotorSpeed  = 0f;
            rightMotorSpeed = 0f;
            averageSpeed    = 0f;
        }

        // Translate forward/backward — runs grounded or airborne to carry momentum.
        if (Mathf.Abs(averageSpeed) > 0.001f)
            rb.MovePosition(rb.position + transform.forward * averageSpeed * dt);

        // Yaw — only while grounded so the bike has ground to push against.
        if (anyGrounded && Mathf.Abs(speedDiff) > 0.001f)
            transform.Rotate(Vector3.up * speedDiff * turnSpeed * dt);
    }

    // -------------------------------------------------------------------------
    // Airborne — extra gravity + self-righting; skipped when grounded
    // -------------------------------------------------------------------------

    private void HandleAirborne(bool anyGrounded)
    {
        if (anyGrounded) return;

        float dt = Time.fixedDeltaTime;

        // Pull the bike down faster to kill floaty high bounces.
        rb.linearVelocity += Vector3.down * extraGravityForce * dt;

        // Rotate back to level — preserves current yaw, corrects roll and pitch only.
        Quaternion upright = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, upright, selfRightSpeed * dt);
    }

    // -------------------------------------------------------------------------
    // Wheel visuals — spin proportional to each motor's speed
    // -------------------------------------------------------------------------

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
        if (wheel == null || Mathf.Abs(speed) < 0.001f) return;
        wheel.Rotate(
            Vector3.right * wheelRotationSpeed * Mathf.Sign(speed) * dt,
            Space.Self);
    }

    // -------------------------------------------------------------------------
    // Ground detection — layer-masked raycasts per wheel
    // -------------------------------------------------------------------------

    private bool IsAnyWheelGrounded(Transform w1, Transform w2)
        => IsWheelGrounded(w1) || IsWheelGrounded(w2);

    private bool IsWheelGrounded(Transform wheel)
    {
        if (wheel == null) return false;
        return Physics.Raycast(
            wheel.position, Vector3.down,
            groundCheckDistance, groundLayerMask);
    }

    // -------------------------------------------------------------------------
    // Respawn — called by RespawnManager to clear all momentum
    // -------------------------------------------------------------------------

    /// <summary>
    /// Zeros both motor speeds and wipes Rigidbody velocity so the bike
    /// starts completely still after a respawn.
    /// </summary>
    public void ResetSpeeds()
    {
        leftMotorSpeed     = 0f;
        rightMotorSpeed    = 0f;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}