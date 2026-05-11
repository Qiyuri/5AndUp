using UnityEngine;

public class MoveAndRotateWheel : MonoBehaviour
{
    [Header("Bikes")]
    public Transform bike1; // First bike (arrow keys)
    public Transform bike2; // Second bike (W/A keys)

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

    [Header("Gronddetectie")]
    [Tooltip("Controleer met bike1 of het wiel contact maakt met de grond.")]
    public float groundCheckDistance = 0.2f;

    float bike1Speed;
    float bike2Speed;

    void Update()
    {
        // Bike 1 - Arrow Keys
        float bike1Target = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
            bike1Target = maxSpeed;
        else if (Input.GetKey(KeyCode.DownArrow))
            bike1Target = -maxSpeed;

        bike1Speed = Mathf.MoveTowards(bike1Speed, bike1Target, acceleration * Time.deltaTime);

        // Bike 2 - W and A keys
        float bike2Target = 0f;
        if (Input.GetKey(KeyCode.W))
            bike2Target = maxSpeed;
        else if (Input.GetKey(KeyCode.A))
            bike2Target = -maxSpeed;

        bike2Speed = Mathf.MoveTowards(bike2Speed, bike2Target, acceleration * Time.deltaTime);

        // Move the bike objects forward
        if (Mathf.Abs(bike1Speed) > 0.001f && bike1 != null && IsWheelGrounded(bike1))
        {
            bike1.Translate(Vector3.forward * bike1Speed * Time.deltaTime, Space.Self);
        }
        if (Mathf.Abs(bike2Speed) > 0.001f && bike2 != null && IsWheelGrounded(bike2))
        {
            bike2.Translate(Vector3.forward * bike2Speed * Time.deltaTime, Space.Self);
        }

        // Move the main bar object based on both bikes
        Vector3 combinedMovement = Vector3.zero;
        if (Mathf.Abs(bike1Speed) > 0.001f)
            combinedMovement += Vector3.forward * bike1Speed * Time.deltaTime;
        if (Mathf.Abs(bike2Speed) > 0.001f)
            combinedMovement += Vector3.forward * bike2Speed * Time.deltaTime;

        if (combinedMovement.sqrMagnitude > 0.0001f)
        {
            transform.Translate(combinedMovement * 0.5f, Space.Self);
        }

        // Rotate both wheels of bike 1
        if (Mathf.Abs(bike1Speed) > 0.001f)
        {
            float rotationAmount = rotationSpeed * Mathf.Sign(bike1Speed) * Time.deltaTime;
            if (bike1_wheel1 != null)
                bike1_wheel1.Rotate(0f, 0f, rotationAmount, Space.Self);
            if (bike1_wheel2 != null)
                bike1_wheel2.Rotate(0f, 0f, rotationAmount, Space.Self);
        }

        // Rotate both wheels of bike 2
        if (Mathf.Abs(bike2Speed) > 0.001f)
        {
            float rotationAmount = rotationSpeed * Mathf.Sign(bike2Speed) * Time.deltaTime;
            if (bike2_wheel1 != null)
                bike2_wheel1.Rotate(0f, 0f, rotationAmount, Space.Self);
            if (bike2_wheel2 != null)
                bike2_wheel2.Rotate(0f, 0f, rotationAmount, Space.Self);
        }
    }

    bool IsWheelGrounded(Transform wheel)
    {
        if (wheel == null)
            return false;

        return Physics.Raycast(wheel.position, Vector3.down, groundCheckDistance);
    }
}
