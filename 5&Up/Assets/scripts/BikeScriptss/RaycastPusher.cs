using UnityEngine;

public class RaycastPusher : MonoBehaviour
{
    [Header("Target Detection")]
    public Transform playerTarget;
    public LayerMask playerLayerMask;

    [Header("Raycast Settings")]
    [Tooltip("How many rays to fire side-by-side.")]
    public int raycastCount = 5;

    [Tooltip("Total width covered by the rays.")]
    public float detectionWidth = 4f;

    public float raycastDistance = 50f;
    public Vector3 raycastDirection = Vector3.forward;

    [Header("Movement")]
    [Tooltip("Maximum speed while moving forward.")]
    public float pushSpeed = 20f;

    [Tooltip("Speed when returning.")]
    public float returnSpeed = 5f;

    [Tooltip("Empty GameObject that marks the furthest forward position.")]
    public Transform endPoint;

    [Tooltip("Distance from player where slowing starts.")]
    public float slowDownDistance = 3f;

    [Tooltip("Minimum percentage of pushSpeed when very close.")]
    [Range(0f, 1f)]
    public float minimumSpeedPercent = 0.35f;

    public float pushCooldown = 2f;

    [Header("Physics")]
    [Tooltip("Force applied to the player's Rigidbody.")]
    public float playerPushForce = 25f;

    private Vector3 originalPosition;
    private bool isMovingForward;
    private bool isReturning;
    private float lastPushTime = -Mathf.Infinity;

    private void Start()
    {
        originalPosition = transform.position;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerTarget = player.transform;
        }
    }

    private void FixedUpdate()
    {
        if (playerTarget == null || endPoint == null)
            return;

        // Start attack if player is detected
        if (!isMovingForward &&
            !isReturning &&
            Time.time > lastPushTime + pushCooldown)
        {
            if (CheckForPlayer())
            {
                isMovingForward = true;
                lastPushTime = Time.time;
            }
        }

        // Move forward
        if (isMovingForward)
        {
            float currentSpeed = pushSpeed;

            float distanceToPlayer =
                Vector3.Distance(transform.position, playerTarget.position);

            // Slow down when close to the player
            if (distanceToPlayer < slowDownDistance)
            {
                float t = distanceToPlayer / slowDownDistance;

                currentSpeed *= Mathf.Lerp(
                    minimumSpeedPercent,
                    1f,
                    t
                );
            }

            Vector3 targetPosition = new Vector3(
                transform.position.x,
                transform.position.y,
                endPoint.position.z
            );

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                currentSpeed * Time.fixedDeltaTime
            );

            if (Mathf.Abs(transform.position.z - endPoint.position.z) < 0.05f)
            {
                isMovingForward = false;
                isReturning = true;
            }
        }
        // Return to start position
        else if (isReturning)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                originalPosition,
                returnSpeed * Time.fixedDeltaTime
            );

            if (Vector3.Distance(transform.position, originalPosition) < 0.05f)
            {
                transform.position = originalPosition;
                isReturning = false;
            }
        }
    }

    private bool CheckForPlayer()
    {
        Vector3 worldDir = transform.TransformDirection(raycastDirection);

        for (int i = 0; i < raycastCount; i++)
        {
            float xOffset = (raycastCount > 1)
                ? ((float)i / (raycastCount - 1) - 0.5f) * detectionWidth
                : 0f;

            Vector3 rayOrigin = transform.position + transform.right * xOffset;

            if (Physics.Raycast(
                rayOrigin,
                worldDir,
                out RaycastHit hit,
                raycastDistance,
                playerLayerMask))
            {
                if (hit.transform == playerTarget)
                    return true;
            }
        }

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform == playerTarget)
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 forceDir = transform.forward;
                forceDir.y = 0.2f;

                rb.AddForce(
                    forceDir * playerPushForce,
                    ForceMode.Impulse
                );
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isReturning ? Color.gray : Color.cyan;

        Vector3 worldDir = transform.TransformDirection(raycastDirection);

        for (int i = 0; i < raycastCount; i++)
        {
            float xOffset = (raycastCount > 1)
                ? ((float)i / (raycastCount - 1) - 0.5f) * detectionWidth
                : 0f;

            Vector3 rayOrigin = transform.position + transform.right * xOffset;

            Gizmos.DrawRay(rayOrigin, worldDir * raycastDistance);
        }

        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(endPoint.position, 0.25f);
        }
    }
}