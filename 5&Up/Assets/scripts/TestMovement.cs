using UnityEngine;

public class TestMovement2 : MonoBehaviour
{
    [Header("Left Bike")]
    public Transform leftBike2; // Left bike (W/S keys)
    public Transform leftBike_frontWheel2;
    public Transform leftBike_rearWheel2;

    [Header("Right Bike")]
    public Transform rightBike2; // Right bike (Arrow keys)
    public Transform rightBike_frontWheel2;
    public Transform rightBike_rearWheel2;

    [Header("Movement")]
    public float maxSpeed2 = 5f;
    public float acceleration2 = 5f;
    public float brakeForce2 = 25f;

    [Header("Rotation")]
    public float wheelRotationSpeed2 = 360f;
    public float turnSpeed2 = 100f;

    [Header("Air Roll Control")]
    public float airRollSpeed = 180f;
    public float airPitchSpeed = 180f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.2f;
    public float nearGroundDistance = 1f;

    [Header("Air Friction")]
    public float airFriction = 0.8f;
    public float airRotationMultiplier = 1.5f;

    private float leftBikeSpeed2;
    private float rightBikeSpeed2;
    private Vector3 currentVelocity2;
    private Rigidbody rb;

    private void Start2()
    {
        currentVelocity2 = Vector3.zero;
    }

    private void Update2()
    {
        HandleInput2();
        HandleMovement2();
        HandleWheelRotation2();
    }

    private void HandleInput2()
    {
        bool isBraking = Input.GetKey(KeyCode.Space);

        // Check ground contact for each bike
        int leftBikeGroundedWheels = CountGroundedWheels(leftBike_frontWheel2, leftBike_rearWheel2);
        int rightBikeGroundedWheels = CountGroundedWheels(rightBike_frontWheel2, rightBike_rearWheel2);

        // Can only control input when wheels are fully grounded (both wheels down)
        bool leftBikeCanAccelerate = leftBikeGroundedWheels >= 2;
        bool rightBikeCanAccelerate = rightBikeGroundedWheels >= 2;

        // Left Bike (W/S keys) - only responsive when grounded
        float leftBikeTarget = 0f;
        if (leftBikeCanAccelerate && !isBraking)
        {
            if (Input.GetKey(KeyCode.W))
                leftBikeTarget = maxSpeed2;
            else if (Input.GetKey(KeyCode.S))
                leftBikeTarget = -maxSpeed2;
        }
        leftBikeSpeed2 = Mathf.MoveTowards(leftBikeSpeed2, leftBikeTarget, (isBraking ? brakeForce2 : acceleration2) * Time.deltaTime);

        // Right Bike (Arrow keys) - only responsive when grounded
        float rightBikeTarget = 0f;
        if (rightBikeCanAccelerate && !isBraking)
        {
            if (Input.GetKey(KeyCode.UpArrow))
                rightBikeTarget = maxSpeed2;
            else if (Input.GetKey(KeyCode.DownArrow))
                rightBikeTarget = -maxSpeed2;
        }
        rightBikeSpeed2 = Mathf.MoveTowards(rightBikeSpeed2, rightBikeTarget, (isBraking ? brakeForce2 : acceleration2) * Time.deltaTime);
    }

    private void HandleMovement2()
    {
        // Check ground contact
        int leftBikeGroundedWheels = CountGroundedWheels(leftBike_frontWheel2, leftBike_rearWheel2);
        int rightBikeGroundedWheels = CountGroundedWheels(rightBike_frontWheel2, rightBike_rearWheel2);

        bool leftBikeHasGroundContact = leftBikeGroundedWheels > 0;
        bool rightBikeHasGroundContact = rightBikeGroundedWheels > 0;
        bool anyBikeGrounded = leftBikeHasGroundContact || rightBikeHasGroundContact;

        // Apply air friction when airborne
        if (!anyBikeGrounded)
        {
            leftBikeSpeed2 *= (1f - (airFriction * Time.deltaTime));
            rightBikeSpeed2 *= (1f - (airFriction * Time.deltaTime));
        }

        float averageSpeed = (leftBikeSpeed2 + rightBikeSpeed2) * 0.5f;
        float speedDifference = leftBikeSpeed2 - rightBikeSpeed2;

        // Move forward with momentum preserved
        if (Mathf.Abs(averageSpeed) > 0.001f)
        {
            transform.Translate(Vector3.forward * averageSpeed * Time.deltaTime);
        }

        // Rotate based on speed difference (only when grounded)
        if (Mathf.Abs(speedDifference) > 0.001f && anyBikeGrounded)
        {
            transform.Rotate(Vector3.up * speedDifference * turnSpeed2 * Time.deltaTime);
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

    private void HandleWheelRotation2()
    {
        int leftBikeGroundedWheels = CountGroundedWheels(leftBike_frontWheel2, leftBike_rearWheel2);
        int rightBikeGroundedWheels = CountGroundedWheels(rightBike_frontWheel2, rightBike_rearWheel2);

        float leftRotationMultiplier = (leftBikeGroundedWheels == 0) ? airRotationMultiplier : 1f;
        float rightRotationMultiplier = (rightBikeGroundedWheels == 0) ? airRotationMultiplier : 1f;

        RotateWheel(leftBike_frontWheel2, leftBikeSpeed2, leftRotationMultiplier);
        RotateWheel(leftBike_rearWheel2, leftBikeSpeed2, leftRotationMultiplier);
        RotateWheel(rightBike_frontWheel2, rightBikeSpeed2, rightRotationMultiplier);
        RotateWheel(rightBike_rearWheel2, rightBikeSpeed2, rightRotationMultiplier);
    }

    private void RotateWheel(Transform wheel, float speed, float rotationMultiplier = 1f)
    {
        if (wheel == null || Mathf.Abs(speed) < 0.001f)
            return;

        float rotationAmount = wheelRotationSpeed2 * Mathf.Sign(speed) * rotationMultiplier * Time.deltaTime;
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