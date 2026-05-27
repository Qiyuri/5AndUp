using UnityEngine;

public class BikeMovement : MonoBehaviour
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

        // Left Bike (W/S keys)
        float leftBikeTarget = 0f;
        if (!isBraking)
        {
            if (Input.GetKey(KeyCode.W))
                leftBikeTarget = maxSpeed;
            else if (Input.GetKey(KeyCode.S))
                leftBikeTarget = -maxSpeed;
        }
        leftBikeSpeed = Mathf.MoveTowards(leftBikeSpeed, leftBikeTarget, (isBraking ? brakeForce : acceleration) * Time.deltaTime);

        // Right Bike (Arrow keys)
        float rightBikeTarget = 0f;
        if (!isBraking)
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
        float averageSpeed = (leftBikeSpeed + rightBikeSpeed) * 0.5f;
        float speedDifference = leftBikeSpeed - rightBikeSpeed;

        // Move forward with momentum preserved
        if (Mathf.Abs(averageSpeed) > 0.001f)
        {
            transform.Translate(Vector3.forward * averageSpeed * Time.deltaTime);
        }

        // Rotate based on speed difference
        if (Mathf.Abs(speedDifference) > 0.001f)
        {
            transform.Rotate(Vector3.up * speedDifference * turnSpeed * Time.deltaTime);
        }
    }

    private void HandleWheelRotation()
    {
        RotateWheel(leftBike_frontWheel, leftBikeSpeed);
        RotateWheel(leftBike_rearWheel, leftBikeSpeed);
        RotateWheel(rightBike_frontWheel, rightBikeSpeed);
        RotateWheel(rightBike_rearWheel, rightBikeSpeed);
    }

    private void RotateWheel(Transform wheel, float speed)
    {
        if (wheel == null || Mathf.Abs(speed) < 0.001f)
            return;

        float rotationAmount = wheelRotationSpeed * Mathf.Sign(speed) * Time.deltaTime;
        wheel.Rotate(Vector3.right * rotationAmount, Space.Self);
    }

}