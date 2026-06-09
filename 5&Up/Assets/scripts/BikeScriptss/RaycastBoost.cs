using UnityEngine;

public class RaycastBoost : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Direction the raycast shoots in.")]
    public Vector3 raycastDirection = Vector3.forward;

    [Tooltip("Length of the raycast.")]
    public float raycastDistance = 50f;

    [Tooltip("Layer mask for detecting the player.")]
    public LayerMask playerLayerMask;

    [Header("Boost")]
    [Tooltip("Speed to boost the player forward.")]
    public float boostSpeed = 30f;

    [Tooltip("Cooldown between boosts (in seconds).")]
    public float boostCooldown = 1f;

    [Header("Visuals")]
    [Tooltip("Draw the raycast in the scene view for debugging.")]
    public bool debugDrawRaycast = true;

    private float lastBoostTime = -Mathf.Infinity;
    private Rigidbody playerRigidbody;

    void Start()
    {
        // Optional: auto-find the player if it has a specific tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerRigidbody = playerObject.GetComponent<Rigidbody>();
        }
    }

    void FixedUpdate()
    {
        CheckAndBoostPlayer();
    }

    private void CheckAndBoostPlayer()
    {
        // Calculate raycast direction in world space
        Vector3 rayDirection = transform.TransformDirection(raycastDirection).normalized;
        Vector3 rayOrigin = transform.position;

        // Check if enough time has passed since last boost
        if (Time.time - lastBoostTime < boostCooldown)
            return;

        // Perform the raycast
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, raycastDistance, playerLayerMask))
        {
            // Check if the hit object has a Rigidbody (the player)
            Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                // Boost the player forward
                Vector3 boostDirection = rayDirection;
                targetRb.linearVelocity = boostDirection * boostSpeed;

                lastBoostTime = Time.time;
                Debug.Log($"Boosted player: {hit.collider.gameObject.name}");
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
        Gizmos.DrawSphere(rayOrigin, 0.2f);

        // Draw a sphere at the end
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(rayEnd, 0.2f);
    }
}
