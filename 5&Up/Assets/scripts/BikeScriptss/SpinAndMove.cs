using UnityEngine;

public class SpinAndMove : MonoBehaviour
{
    [Header("Spinning")]
    [Tooltip("Enable spinning.")]
    public bool enableSpin = true;

    [Tooltip("Rotation axis.")]
    public Vector3 spinAxis = Vector3.up;

    [Tooltip("Rotation speed in degrees per second.")]
    public float spinSpeed = 90f;

    [Header("Travel Points")]
    [Tooltip("First point to travel to.")]
    public Transform pointA;

    [Tooltip("Second point to travel to.")]
    public Transform pointB;

    [Header("Movement")]
    [Tooltip("Speed of movement towards point B.")]
    public float forwardSpeed = 2f;

    [Tooltip("Speed of movement back to point A (faster return).")]
    public float backwardSpeed = 5f;

    [Tooltip("Acceleration when moving forward.")]
    public float forwardAcceleration = 1f;

    private Vector3 startPosition;
    private bool movingToB = true;
    private float currentForwardSpeed = 0f;

    void Start()
    {
        // Store initial position
        startPosition = transform.position;
    }

    void Update()
    {
        // Handle spinning
        if (enableSpin)
        {
            transform.Rotate(spinAxis.normalized * spinSpeed * Time.deltaTime);
        }

        if (pointA == null || pointB == null)
            return;

        // Determine target position
        Vector3 targetPosition = movingToB ? pointB.position : pointA.position;
        
        // Determine current speed
        float currentSpeed;
        if (movingToB)
        {
            // Moving forward - accelerate up to forwardSpeed
            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, forwardSpeed, forwardAcceleration * Time.deltaTime);
            currentSpeed = currentForwardSpeed;
        }
        else
        {
            // Moving backward - use backwardSpeed (faster return)
            currentForwardSpeed = 0f; // Reset acceleration for next forward movement
            currentSpeed = backwardSpeed;
        }
        
        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

        // Check if reached target, then switch direction
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            movingToB = !movingToB;
        }
    }
}
