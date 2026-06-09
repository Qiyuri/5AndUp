using UnityEngine;

public class RaycastPusher : MonoBehaviour
{
    [Header("Target Detection")]
    [Tooltip("The player object to track.")]
    public Transform playerTarget;

    [Header("Raycast Detection")]
    [Tooltip("Direction the raycast shoots in.")]
    public Vector3 raycastDirection = Vector3.forward;

    [Tooltip("Offset of the raycast on the X axis.")]
    public float raycastOffsetX = 0f;

    [Tooltip("Length of the raycast.")]
    public float raycastDistance = 50f;

    [Tooltip("Layer mask for detecting the player.")]
    public LayerMask playerLayerMask;

    [Header("Push Movement")]
    [Tooltip("Speed to move toward the player.")]
    public float moveSpeed = 10f;

    [Tooltip("Speed to push forward when hitting player.")]
    public float pushSpeed = 15f;

    [Tooltip("Distance to travel forward when pushing.")]
    public float pushDistance = 5f;

    [Header("Return Movement")]
    [Tooltip("Speed to return to original position.")]
    public float returnSpeed = 3f;

    [Header("Cooldown")]
    [Tooltip("Cooldown between pushes (in seconds).")]
    public float pushCooldown = 2f;

    [Header("Detection")]
    [Tooltip("Distance to player before attempting push.")]
    public float pushDetectionDistance = 2f;

    [Header("Visuals")]
    [Tooltip("Draw the raycast in the scene view for debugging.")]
    public bool debugDrawRaycast = true;

    private Vector3 originalPosition;
    private bool isMovingToPlayer = false;
    private bool isMovingForward = false;
    private bool isReturning = false;
    private float lastPushTime = -Mathf.Infinity;

    void Start()
    {
        originalPosition = transform.position;

        // Auto-find player if not assigned
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTarget = playerObject.transform;
        }
    }

    void FixedUpdate()
    {
        if (playerTarget == null)
            return;

        HandleMovement();
        CheckAndPush();
    }

    private void CheckAndPush()
    {
        // Don't check if already moving or if cooldown hasn't passed
        if (isMovingToPlayer || isMovingForward || isReturning || Time.time - lastPushTime < pushCooldown)
            return;

        // Cast raycast to detect player
        Vector3 rayDirection = transform.TransformDirection(raycastDirection).normalized;
        Vector3 rayOrigin = transform.position + transform.right * raycastOffsetX;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, raycastDistance, playerLayerMask))
        {
            // Check if the hit object is the player
            if (hit.transform == playerTarget)
            {
                // Start moving towards player
                isMovingToPlayer = true;
                lastPushTime = Time.time;
                Debug.Log("Player touched raycast! Starting push sequence.");
            }
        }
    }

    private void HandleMovement()
    {
        if (isMovingToPlayer)
        {
            // Move to player's Z coordinate
            Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, playerTarget.position.z);
            transform.position = Vector3.MoveTowards(
                transform.position, targetPosition,
                moveSpeed * Time.fixedDeltaTime);

            // Check if reached player's Z coordinate
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isMovingToPlayer = false;
                isMovingForward = true;
            }
        }
        else if (isMovingForward)
        {
            // Move forward to push the player
            Vector3 pushTarget = transform.position + transform.forward * pushDistance;
            transform.position = Vector3.MoveTowards(
                transform.position, pushTarget,
                pushSpeed * Time.fixedDeltaTime);

            // Check if reached push distance
            if (Vector3.Distance(transform.position, pushTarget) < 0.1f)
            {
                isMovingForward = false;
                isReturning = true;

                // Push the player
                if (playerTarget != null)
                {
                    Rigidbody playerRb = playerTarget.GetComponent<Rigidbody>();
                    if (playerRb != null)
                    {
                        playerRb.linearVelocity = transform.forward * pushSpeed;
                    }
                }
            }
        }
        else if (isReturning)
        {
            // Return to original position
            transform.position = Vector3.MoveTowards(
                transform.position, originalPosition,
                returnSpeed * Time.fixedDeltaTime);

            // Check if returned to original position
            if (Vector3.Distance(transform.position, originalPosition) < 0.1f)
            {
                isReturning = false;
                transform.position = originalPosition;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!debugDrawRaycast) return;

        Vector3 rayDirection = transform.TransformDirection(raycastDirection).normalized;
        Vector3 rayOrigin = transform.position;
        Vector3 rayEnd = rayOrigin + rayDirection * raycastDistance;

        // Draw the raycast as a line
        Gizmos.color = Color.green;
        Gizmos.DrawLine(rayOrigin, rayEnd);

        // Draw a sphere at the start
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(rayOrigin, 0.3f);

        // Draw a sphere at the end
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(rayEnd, 0.3f);

        // Draw original position
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(originalPosition, 0.3f);
    }
}
