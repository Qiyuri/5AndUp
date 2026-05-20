using UnityEngine;

public enum WheelRotationAxis
{
    X,
    Y,
    Z
}

public class MoveAndRotateWheel : MonoBehaviour
{
    [Header("Bikes")]
    public Transform bike1; // Left bike (W/S keys)
    public Transform bike2; // Right bike (Arrow keys)

    [Header("Wheels of Bike 1")]
    public Transform bike1_wheel1; // First wheel of bike1
    public Transform bike1_wheel2; // Second wheel of bike1

    [Header("Wheels of Bike 2")]
    public Transform bike2_wheel1; // First wheel of bike2
    public Transform bike2_wheel2; // Second wheel of bike2

    [Header("Movement")]
    [Tooltip("Maximum speed of the vehicle.")]
    public float maxSpeed = 5f;

    [Tooltip("How fast the vehicle accelerates.")]
    public float acceleration = 5f;

    [Tooltip("How fast the vehicle brakes when spacebar is pressed (hard brake).")]
    public float brakeForce = 25f;

    [Header("Rotation")]
    public float rotationSpeed = 360f;

    [Tooltip("How fast the vehicle rotates at different speeds (tank movement).")]
    public float turnSpeed = 100f;

    [Tooltip("The axis around which the wheels rotate.")]
    public WheelRotationAxis wheelRotationAxis = WheelRotationAxis.Z;

    [Header("Ground Detection")]
    [Tooltip("Check if the wheels make contact with the ground.")]
    public float groundCheckDistance = 0.2f;

    [Header("Air Friction")]
    [Tooltip("Friction applied when airborne (0-1, higher = more drag).")]
    public float airFriction = 0.8f;

    float bike1Speed;
    float bike2Speed;

    void Update()
    {
        bool isBraking = Input.GetKey(KeyCode.Space);

        // Check ground contact for each bike
        int bike1GroundedWheels = CountGroundedWheels(bike1_wheel1, bike1_wheel2);
        int bike2GroundedWheels = CountGroundedWheels(bike2_wheel1, bike2_wheel2);
        
        // Can only accelerate when BOTH wheels are grounded
        // 2 wheels = full power, less than 2 wheels = no acceleration
        float bike1AccelMultiplier = bike1GroundedWheels >= 2 ? 1f : 0f;
        float bike2AccelMultiplier = bike2GroundedWheels >= 2 ? 1f : 0f;

        // Bike 1 - W and S keys (left side)
        float bike1Target = 0f;
        if (!isBraking)
        {
            if (Input.GetKey(KeyCode.W))
                bike1Target = maxSpeed;
            else if (Input.GetKey(KeyCode.S))
                bike1Target = -maxSpeed;
        }

        float bike1Accel = isBraking ? brakeForce : (acceleration * bike1AccelMultiplier);
        bike1Speed = Mathf.MoveTowards(bike1Speed, bike1Target, bike1Accel * Time.deltaTime);

        // Bike 2 - Arrow Keys (right side)
        float bike2Target = 0f;
        if (!isBraking)
        {
            if (Input.GetKey(KeyCode.UpArrow))
                bike2Target = maxSpeed;
            else if (Input.GetKey(KeyCode.DownArrow))
                bike2Target = -maxSpeed;
        }

        float bike2Accel = isBraking ? brakeForce : (acceleration * bike2AccelMultiplier);
        bike2Speed = Mathf.MoveTowards(bike2Speed, bike2Target, bike2Accel * Time.deltaTime);

        // Hamsteria-style movement: average speed for forward, difference for rotation
        float averageSpeed = (bike1Speed + bike2Speed) * 0.5f;
        float speedDifference = bike1Speed - bike2Speed;

        bool bike1HasGroundContact = bike1GroundedWheels > 0;
        bool bike2HasGroundContact = bike2GroundedWheels > 0;
        bool anyBikeGrounded = bike1HasGroundContact || bike2HasGroundContact;

        // Apply air friction when airborne to prevent endless sliding
        if (!anyBikeGrounded)
        {
            bike1Speed *= (1f - (airFriction * Time.deltaTime));
            bike2Speed *= (1f - (airFriction * Time.deltaTime));
            averageSpeed = (bike1Speed + bike2Speed) * 0.5f;
        }

        // Move forward with current momentum (even when airborne)
        if (Mathf.Abs(averageSpeed) > 0.001f)
        {
            Vector3 moveDir = transform.TransformDirection(Vector3.forward);
            transform.position += moveDir * averageSpeed * Time.deltaTime;
        }

        // Rotate around the opposite bike only if at least one has ground contact
        if (Mathf.Abs(speedDifference) > 0.001f && anyBikeGrounded)
        {
            Vector3 pivotPoint = GetHamsteriaRotationPivot();
            transform.RotateAround(pivotPoint, Vector3.up, speedDifference * turnSpeed * Time.deltaTime);
        }

        // Rotate both wheels of bike 1 (regardless of ground contact)
        if (Mathf.Abs(bike1Speed) > 0.001f)
        {
            float rotationAmount = rotationSpeed * Mathf.Sign(bike1Speed) * Time.deltaTime;
            Vector3 axis = GetRotationAxis(wheelRotationAxis);
            if (bike1_wheel1 != null)
                bike1_wheel1.Rotate(axis * rotationAmount, Space.Self);
            if (bike1_wheel2 != null)
                bike1_wheel2.Rotate(axis * rotationAmount, Space.Self);
        }

        // Rotate both wheels of bike 2 (regardless of ground contact)
        if (Mathf.Abs(bike2Speed) > 0.001f)
        {
            float rotationAmount = rotationSpeed * Mathf.Sign(bike2Speed) * Time.deltaTime;
            Vector3 axis = GetRotationAxis(wheelRotationAxis);
            if (bike2_wheel1 != null)
                bike2_wheel1.Rotate(axis * rotationAmount, Space.Self);
            if (bike2_wheel2 != null)
                bike2_wheel2.Rotate(axis * rotationAmount, Space.Self);
        }
    }

    int CountGroundedWheels(Transform wheel1, Transform wheel2)
    {
        int count = 0;
        if (IsWheelGrounded(wheel1)) count++;
        if (IsWheelGrounded(wheel2)) count++;
        return count;
    }

    Vector3 GetHamsteriaRotationPivot()
    {
        bool bike1Moving = Mathf.Abs(bike1Speed) > 0.001f;
        bool bike2Moving = Mathf.Abs(bike2Speed) > 0.001f;

        // If only one wheel is moving, rotate around the other
        if (bike1Moving && !bike2Moving && bike2 != null)
            return bike2.position;
        if (bike2Moving && !bike1Moving && bike1 != null)
            return bike1.position;

        // If both are moving, rotate around the slower one
        if (Mathf.Abs(bike1Speed) < Mathf.Abs(bike2Speed) && bike1 != null)
            return bike1.position;
        if (bike2 != null)
            return bike2.position;

        return transform.position;
    }

    bool IsWheelGrounded(Transform wheel)
    {
        if (wheel == null)
            return false;

        return Physics.Raycast(wheel.position, Vector3.down, groundCheckDistance);
    }

    Vector3 GetRotationAxis(WheelRotationAxis axis)
    {
        return axis switch
        {
            WheelRotationAxis.X => Vector3.right,
            WheelRotationAxis.Y => Vector3.up,
            WheelRotationAxis.Z => Vector3.forward,
            _ => Vector3.forward
        };
    }

    // Public method to reset speeds (called by RespawnManager)
    public void ResetSpeeds()
    {
        bike1Speed = 0f;
        bike2Speed = 0f;
    }
}
