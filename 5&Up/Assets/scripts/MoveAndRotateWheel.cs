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

    [Header("Wielen van Bike 1")]
    public Transform bike1_wheel1; // First wheel of bike1
    public Transform bike1_wheel2; // Second wheel of bike1

    [Header("Wielen van Bike 2")]
    public Transform bike2_wheel1; // First wheel of bike2
    public Transform bike2_wheel2; // Second wheel of bike2

    [Header("Beweging")]
    [Tooltip("Maximale snelheid van het voertuig.")]
    public float maxSpeed = 5f;

    [Tooltip("Hoe snel het voertuig accelereert.")]
    public float acceleration = 5f;

    [Header("Rotatie")]
    public float rotationSpeed = 360f;

    [Tooltip("Hoe snel het voertuig draait bij verschillende snelheden (tank beweging).")]
    public float turnSpeed = 100f;

    [Tooltip("De as waarom de wielen draaien.")]
    public WheelRotationAxis wheelRotationAxis = WheelRotationAxis.Z;

    [Header("Gronddetectie")]
    [Tooltip("Controleer of de wielen contact maken met de grond.")]
    public float groundCheckDistance = 0.2f;

    float bike1Speed;
    float bike2Speed;

    void Update()
    {
        // Bike 1 - W and S keys (left side)
        float bike1Target = 0f;
        if (Input.GetKey(KeyCode.W))
            bike1Target = maxSpeed;
        else if (Input.GetKey(KeyCode.S))
            bike1Target = -maxSpeed;

        bike1Speed = Mathf.MoveTowards(bike1Speed, bike1Target, acceleration * Time.deltaTime);

        // Bike 2 - Arrow Keys (right side)
        float bike2Target = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
            bike2Target = maxSpeed;
        else if (Input.GetKey(KeyCode.DownArrow))
            bike2Target = -maxSpeed;

        bike2Speed = Mathf.MoveTowards(bike2Speed, bike2Target, acceleration * Time.deltaTime);

        // Hamsteria-style movement: average speed for forward, difference for rotation
        float averageSpeed = (bike1Speed + bike2Speed) * 0.5f;
        float speedDifference = bike1Speed - bike2Speed;

        bool bike1Grounded = IsWheelGrounded(bike1);
        bool bike2Grounded = IsWheelGrounded(bike2);

        // Move forward if at least one bike is grounded
        if (Mathf.Abs(averageSpeed) > 0.001f && (bike1Grounded || bike2Grounded))
        {
            Vector3 moveDir = transform.TransformDirection(Vector3.forward);
            transform.position += moveDir * averageSpeed * Time.deltaTime;
        }

        // Rotate around the opposite bike based on which one is being moved
        if (Mathf.Abs(speedDifference) > 0.001f && (bike1Grounded || bike2Grounded))
        {
            bool bike1Moving = Mathf.Abs(bike1Speed) > 0.001f;
            bool bike2Moving = Mathf.Abs(bike2Speed) > 0.001f;

            if (bike1Moving && bike2 != null)
            {
                // Bike1 is moving -> rotate around bike2
                transform.RotateAround(bike2.position, Vector3.up, speedDifference * turnSpeed * Time.deltaTime);
            }
            else if (bike2Moving && bike1 != null)
            {
                // Bike2 is moving -> rotate around bike1
                transform.RotateAround(bike1.position, Vector3.up, speedDifference * turnSpeed * Time.deltaTime);
            }
        }

        // Rotate both wheels of bike 1
        if (Mathf.Abs(bike1Speed) > 0.001f)
        {
            float rotationAmount = rotationSpeed * Mathf.Sign(bike1Speed) * Time.deltaTime;
            Vector3 axis = GetRotationAxis(wheelRotationAxis);
            if (bike1_wheel1 != null)
                bike1_wheel1.Rotate(axis * rotationAmount, Space.Self);
            if (bike1_wheel2 != null)
                bike1_wheel2.Rotate(axis * rotationAmount, Space.Self);
        }

        // Rotate both wheels of bike 2
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
}
