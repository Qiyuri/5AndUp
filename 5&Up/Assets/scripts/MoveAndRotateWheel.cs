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
    public Transform bike1; // First bike (arrow keys)
    public Transform bike2; // Second bike (W/S keys)

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
    [Tooltip("Controleer met bike1 of het wiel contact maakt met de grond.")]
    public float groundCheckDistance = 0.2f;

    float bike1Speed;
    float bike2Speed;

    void Update()
    {
        // Bike 1 - W and S keys
        float bike1Target = 0f;
        if (Input.GetKey(KeyCode.W))
            bike1Target = maxSpeed;
        else if (Input.GetKey(KeyCode.S))
            bike1Target = -maxSpeed;

        bike1Speed = Mathf.MoveTowards(bike1Speed, bike1Target, acceleration * Time.deltaTime);

        // Bike 2 - Arrow Keys
        float bike2Target = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
            bike2Target = maxSpeed;
        else if (Input.GetKey(KeyCode.DownArrow))
            bike2Target = -maxSpeed;

        bike2Speed = Mathf.MoveTowards(bike2Speed, bike2Target, acceleration * Time.deltaTime);

        // Tank-like movement: forward move and pivoting
        bool bike1Active = Mathf.Abs(bike1Target) > 0.001f;
        bool bike2Active = Mathf.Abs(bike2Target) > 0.001f;

        if (!bike1Active && bike2Active && bike1 != null)
        {
            // Arrow keys only -> rotate around bike1
            float turnDirection = bike2Target > 0f ? 1f : -1f;
            transform.RotateAround(bike1.position, Vector3.up, turnDirection * turnSpeed * Time.deltaTime);
        }
        else if (!bike2Active && bike1Active && bike2 != null)
        {
            // W/S only -> rotate around bike2
            float turnDirection = bike1Target < 0f ? 1f : -1f;
            transform.RotateAround(bike2.position, Vector3.up, turnDirection * turnSpeed * Time.deltaTime);
        }
        else
        {
            float averageSpeed = (bike1Speed + bike2Speed) * 0.5f;
            float speedDifference = bike1Speed - bike2Speed;

            if (Mathf.Abs(averageSpeed) > 0.001f && (IsWheelGrounded(bike1) || IsWheelGrounded(bike2)))
            {
                transform.Translate(Vector3.forward * averageSpeed * Time.deltaTime, Space.Self);
            }

            if (Mathf.Abs(speedDifference) > 0.001f && (IsWheelGrounded(bike1) || IsWheelGrounded(bike2)))
            {
                if (speedDifference > 0 && bike2 != null)
                {
                    transform.RotateAround(bike2.position, Vector3.up, speedDifference * turnSpeed * Time.deltaTime);
                }
                else if (speedDifference < 0 && bike1 != null)
                {
                    transform.RotateAround(bike1.position, Vector3.up, -speedDifference * turnSpeed * Time.deltaTime);
                }
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
