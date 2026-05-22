using UnityEngine;

public class TestMovement : MonoBehaviour
{
    [Header("Left Bike")]
    public Transform leftBike; // Left bike (W/S keys)
    public Transform leftBike_frontWheel;
    public Transform leftBike_rearWheel;

    [Header("Right Bike")]
    public Transform rightBike; // Right bike (Arrow keys)
    public Transform rightBike_frontWheel;
    public Transform rightBike_rearWheel;

    [Header("Movement")]
    public float maxSpeed = 5f;
    public float acceleration = 5f;
    public float brakeForce = 25f;

    [Header("Rotation")]
    public float wheelRotationSpeed = 360f;
    public float turnSpeed = 100f;

    [Header("Air Roll Control")]
    public float airRollSpeed = 180f;
    public float airPitchSpeed = 180f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.2f;
    public float nearGroundDistance = 1f;

    [Header("Air Friction")]
    public float airFriction = 0.8f;
    public float airRotationMultiplier = 1.5f;

    private float leftBikeSpeed;
    private float rightBikeSpeed;
    private Vector3 currentVelocity;
    private Rigidbody rb;

    private void Start()
    {
        currentVelocity = Vector3.zero;
    }

    private void Update()
    {
        HandleInput();
        HandleMovement();
        HandleWheelRotation();
    }

    private void HandleInput()
    {
        bool isBraking = Input.GetKey(KeyCode.Space);

        // Check ground contact for each bike
        int leftBikeGroundedWheels = CountGroundedWheels(leftBike_frontWheel, leftBike_rearWheel);
        int rightBikeGroundedWheels = CountGroundedWheels(rightBike_frontWheel, rightBike_rearWheel);

        // Can only control input when wheels are fully grounded (both wheels down)
        bool leftBikeCanAccelerate = leftBikeGroundedWheels >= 2;
        bool rightBikeCanAccelerate = rightBikeGroundedWheels >= 2;

        // Left Bike (W/S keys) - only responsive when grounded
        float leftBikeTarget = 0f;
        if (leftBikeCanAccelerate && !isBraking)
        {
            if (Input.GetKey(KeyCode.W))
                leftBikeTarget = maxSpeed;
            else if (Input.GetKey(KeyCode.S))
                leftBikeTarget = -maxSpeed;
        }
        leftBikeSpeed = Mathf.MoveTowards(leftBikeSpeed, leftBikeTarget, (isBraking ? brakeForce : acceleration) * Time.deltaTime);

        // Right Bike (Arrow keys) - only responsive when grounded
        float rightBikeTarget = 0f;
        if (rightBikeCanAccelerate && !isBraking)
        {
            if (Input.GetKey(KeyCode.UpArrow))
                rightBikeTarget = maxSpeed;
            else if (Input.GetKey(KeyCode.DownArrow))
                rightBikeTarget = -maxSpeed;
        }
        rightBikeSpeed = Mathf.MoveTowards(rightBikeSpeed, rightBikeTarget, (isBraking ? brakeForce : acceleration) * Time.deltaTime);
    }

    private void HandleMovement()
    {
        // Check ground contact
        int leftBikeGroundedWheels = CountGroundedWheels(leftBike_frontWheel, leftBike_rearWheel);
        int rightBikeGroundedWheels = CountGroundedWheels(rightBike_frontWheel, rightBike_rearWheel);
        
        bool leftBikeHasGroundContact = leftBikeGroundedWheels > 0;
        bool rightBikeHasGroundContact = rightBikeGroundedWheels > 0;
        bool anyBikeGrounded = leftBikeHasGroundContact || rightBikeHasGroundContact;

        // Apply air friction when airborne
        if (!anyBikeGrounded)
        {
            leftBikeSpeed *= (1f - (airFriction * Time.deltaTime));
            rightBikeSpeed *= (1f - (airFriction * Time.deltaTime));
        }

        float averageSpeed = (leftBikeSpeed + rightBikeSpeed) * 0.5f;
        float speedDifference = leftBikeSpeed - rightBikeSpeed;

        // Move forward with momentum preserved
        if (Mathf.Abs(averageSpeed) > 0.001f)
        {
            transform.Translate(Vector3.forward * averageSpeed * Time.deltaTime);
        }

        // Rotate based on speed difference (only when grounded)
        if (Mathf.Abs(speedDifference) > 0.001f && anyBikeGrounded)
        {
            transform.Rotate(Vector3.up * speedDifference * turnSpeed * Time.deltaTime);
        }

        // Air control (only when airborne)
        if (!anyBikeGrounded)
        {
            float leftInput = 0f;
            if (Input.GetKey(KeyCode.W))
                leftInput = 1f;
            else if (Input.GetKey(KeyCode.S))
                leftInput = -1f;

            float rightInput = 0f;
            if (Input.GetKey(KeyCode.UpArrow))
                rightInput = 1f;
            else if (Input.GetKey(KeyCode.DownArrow))
                rightInput = -1f;

            // Roll around local forward axis based on left/right input difference
            float rollInput = rightInput - leftInput;
            if (Mathf.Abs(rollInput) > 0.001f)
            {
                transform.Rotate(Vector3.forward * rollInput * airRollSpeed * Time.deltaTime);
            }

            // Pitch forward/backward based on combined input
            float pitchInput = leftInput + rightInput;
            if (Mathf.Abs(pitchInput) > 0.001f)
            {
                transform.Rotate(Vector3.right * pitchInput * airPitchSpeed * Time.deltaTime);
            }
        }
    }

    private void HandleWheelRotation()
    {
        int leftBikeGroundedWheels = CountGroundedWheels(leftBike_frontWheel, leftBike_rearWheel);
        int rightBikeGroundedWheels = CountGroundedWheels(rightBike_frontWheel, rightBike_rearWheel);

        float leftRotationMultiplier = (leftBikeGroundedWheels == 0) ? airRotationMultiplier : 1f;
        float rightRotationMultiplier = (rightBikeGroundedWheels == 0) ? airRotationMultiplier : 1f;

        RotateWheel(leftBike_frontWheel, leftBikeSpeed, leftRotationMultiplier);
        RotateWheel(leftBike_rearWheel, leftBikeSpeed, leftRotationMultiplier);
        RotateWheel(rightBike_frontWheel, rightBikeSpeed, rightRotationMultiplier);
        RotateWheel(rightBike_rearWheel, rightBikeSpeed, rightRotationMultiplier);
    }

    private void RotateWheel(Transform wheel, float speed, float rotationMultiplier = 1f)
    {
        if (wheel == null || Mathf.Abs(speed) < 0.001f)
            return;

        float rotationAmount = wheelRotationSpeed * Mathf.Sign(speed) * rotationMultiplier * Time.deltaTime;
        wheel.Rotate(Vector3.right * rotationAmount, Space.Self);
    }

    private int CountGroundedWheels(Transform wheel1, Transform wheel2)
    {
        int count = 0;
        if (IsWheelGrounded(wheel1)) count++;
        if (IsWheelGrounded(wheel2)) count++;
        return count;
    }

    private bool IsWheelGrounded(Transform wheel)
    {
        if (wheel == null)
            return false;

        return Physics.Raycast(wheel.position, Vector3.down, groundCheckDistance);
    }
}