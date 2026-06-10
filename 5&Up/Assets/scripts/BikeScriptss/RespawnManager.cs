using System.Collections;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Time required to hold F for respawn.")]
    public float respawnHoldTime = 1f;

    [Header("Audio")]
    [Tooltip("Sound to play when respawning.")]
    public AudioClip respawnSound;

    private Vector3     spawnPosition;
    private Quaternion  spawnRotation;
    private float       holdTimer = 0f;
    private AudioSource audioSource;
    private Rigidbody   rb;

    void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= respawnHoldTime)
            {
                Respawn();
                holdTimer = 0f;
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    void Respawn()
    {
        RespawnToPosition(spawnPosition, spawnRotation);
    }

    /// <summary>
    /// Respawn to a specific position and rotation.
    /// Can be called from a CheckpointManager or any other system.
    /// </summary>
    public void RespawnToPosition(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;

        if (audioSource != null && respawnSound != null)
            audioSource.PlayOneShot(respawnSound);

        StartCoroutine(DoRespawn(position, rotation));
    }

    /// <summary>
    /// Set the spawn point without moving the player immediately.
    /// </summary>
    public void SetSpawnPoint(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;

        Debug.Log("Spawn point updated to new checkpoint.");
    }

    // Teleports via the Rigidbody so the physics engine is always in sync,
    // regardless of framerate. Kinematic for one fixed frame ensures no
    // residual velocity or collision response carries over from the old position.
    private IEnumerator DoRespawn(Vector3 position, Quaternion rotation)
    {
        // Stop all movement first
        TestMovement moveScript = GetComponent<TestMovement>();
        if (moveScript != null)
            moveScript.ResetSpeeds();

        if (rb != null)
        {
            // Go kinematic: physics engine stops simulating this body
            bool wasKinematic  = rb.isKinematic;
            rb.isKinematic     = true;

            // Move via Rigidbody — this is guaranteed to be in sync with
            // the physics engine unlike setting transform.position directly
            rb.position        = position;
            rb.rotation        = rotation;

            // Wait for the physics engine to process exactly one FixedUpdate
            // at the new position before re-enabling simulation
            yield return new WaitForFixedUpdate();

            rb.isKinematic     = wasKinematic;

            // Zero out any velocity the solver may have accumulated
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            // Fallback if no Rigidbody
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}