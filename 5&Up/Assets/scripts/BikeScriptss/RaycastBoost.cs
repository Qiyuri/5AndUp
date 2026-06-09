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

    [Tooltip("Number of raycasts side by side.")]
    public int raycastCount = 3;

    [Tooltip("Spacing between each raycast on the X axis.")]
    public float raycastSpacing = 0.5f;

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
        Vector3 rayDirection = transform.TransformDirection(raycastDirection).normalized;

        // Check cooldown
        if (Time.time - lastBoostTime < boostCooldown)
            return;

        // Cast multiple raycasts side by side
        for (int i = 0; i < raycastCount; i++)
        {
            float offset = (i - (raycastCount - 1) / 2f) * raycastSpacing;
            Vector3 rayOrigin = transform.position + transform.right * offset;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, raycastDistance, playerLayerMask))
            {
                Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    targetRb.linearVelocity = rayDirection * boostSpeed;

                    lastBoostTime = Time.time;
                    Debug.Log($"Boosted player: {hit.collider.gameObject.name}");
                    break; // Stop after first hit
                }
            }

            if (debugDrawRaycast)
            {
                Debug.DrawRay(rayOrigin, rayDirection * raycastDistance, Color.green);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!debugDrawRaycast) return;

        Vector3 rayDirection = transform.TransformDirection(raycastDirection).normalized;

        for (int i = 0; i < raycastCount; i++)
        {
            float offset = (i - (raycastCount - 1) / 2f) * raycastSpacing;
            Vector3 rayOrigin = transform.position + transform.right * offset;
            Vector3 rayEnd = rayOrigin + rayDirection * raycastDistance;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(rayOrigin, rayEnd);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(rayOrigin, 0.2f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(rayEnd, 0.2f);
        }
    }
}
