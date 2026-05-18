using UnityEngine;
using System.Collections.Generic;

public class SpinAndMove : MonoBehaviour
{
    [Header("Spinning")]
    [Tooltip("Enable spinning.")]
    public bool enableSpin = true;

    [Tooltip("Rotation axis.")]
    public Vector3 spinAxis = Vector3.up;

    [Tooltip("Rotation speed in degrees per second.")]
    public float spinSpeed = 90f;

    [Header("Movement")]
    [Tooltip("Enable movement between positions.")]
    public bool enableMovement = true;

    [Tooltip("Path of waypoints to follow (at least 2).")]
    public Transform[] waypoints = new Transform[2];

    [Tooltip("Speed of movement.")]
    public float moveSpeed = 2f;

    [Tooltip("Time to wait at each waypoint (0 = no wait).")]
    public float waitTimeAtWaypoint = 0f;

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    void Update()
    {
        // Handle spinning
        if (enableSpin)
        {
            transform.Rotate(spinAxis.normalized * spinSpeed * Time.deltaTime);
        }

        // Handle movement between waypoints
        if (enableMovement && waypoints != null && waypoints.Length >= 2)
        {
            if (isWaiting)
            {
                // Wait at waypoint
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    // Move to next waypoint
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                }
            }
            else
            {
                // Move towards current waypoint
                Transform targetWaypoint = waypoints[currentWaypointIndex];
                if (targetWaypoint != null)
                {
                    float distance = Vector3.Distance(transform.position, targetWaypoint.position);

                    // Move towards target
                    transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, moveSpeed * Time.deltaTime);

                    // Check if reached waypoint
                    if (distance < 0.1f)
                    {
                        if (waitTimeAtWaypoint > 0f)
                        {
                            isWaiting = true;
                            waitTimer = waitTimeAtWaypoint;
                        }
                        else
                        {
                            // No wait time, move to next waypoint immediately
                            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                        }
                    }
                }
            }
        }
    }
}
