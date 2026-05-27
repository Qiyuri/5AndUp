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

    [Header("Hitbox Trigger")]
    [Tooltip("The object to monitor distance from (e.g., the player car).")]
    public Transform targetObject;

    [Tooltip("Distance from targetObject to trigger movement.")]
    public float triggerDistance = 5f;

    [Header("Movement")]
    [Tooltip("Speed of movement towards and from trigger.")]
    public float moveSpeed = 2f;

    [Tooltip("Time to wait at trigger object.")]
    public float waitTimeAtTrigger = 1f;

    private Vector3 startPosition;
    private bool isMovingToTrigger = false;
    private bool isWaitingAtTrigger = false;
    private bool isMovingBack = false;
    private float waitTimer = 0f;

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

        if (targetObject == null)
            return;

        // Check distance from target object to this object
        float distanceToTarget = Vector3.Distance(targetObject.position, transform.position);

        // Handle waiting at position
        if (isWaitingAtTrigger)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaitingAtTrigger = false;
                isMovingBack = true;
            }
        }
        // Handle moving towards trigger position
        else if (isMovingToTrigger)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPosition, moveSpeed * Time.deltaTime);

            // Check if returned to start
            if (distanceToTarget < 0.5f)
            {
                isMovingToTrigger = false;
                isWaitingAtTrigger = true;
                waitTimer = waitTimeAtTrigger;
            }
        }
        // Handle moving away from target
        else if (isMovingBack)
        {
            Vector3 awayDirection = (transform.position - targetObject.position).normalized;
            Vector3 moveAwayPos = transform.position + awayDirection * moveSpeed * Time.deltaTime;
            transform.position = moveAwayPos;

            // Check if far enough
            if (distanceToTarget > triggerDistance + 2f)
            {
                isMovingBack = false;
            }
        }
        // Check if target is close enough to trigger activation
        else if (!isMovingToTrigger && !isMovingBack && distanceToTarget < triggerDistance)
        {
            isMovingToTrigger = true;
        }
    }
}
