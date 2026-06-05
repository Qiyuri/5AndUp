using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Time required to hold F for respawn.")]
    public float respawnHoldTime = 1f;

    [Header("Audio")]
    [Tooltip("Sound to play when respawning.")]
    public AudioClip respawnSound;

    private Vector3    spawnPosition;
    private Quaternion spawnRotation;
    private float      holdTimer = 0f;
    private AudioSource audioSource;

    void Start()
    {
        // Store the initial position and rotation as the spawn point.
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
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
        // Update stored spawn point so the next manual respawn
        // returns here rather than the original start position.
        spawnPosition = position;
        spawnRotation = rotation;

        // Reposition the bike.
        transform.position = position;
        transform.rotation = rotation;

        // Play respawn sound.
        if (audioSource != null && respawnSound != null)
            audioSource.PlayOneShot(respawnSound);

        // Clear all momentum so the bike starts still.
        TestMovement moveScript = GetComponent<TestMovement>();
        if (moveScript != null)
            moveScript.ResetSpeeds();
    }

    /// <summary>
    /// Set the spawn point without moving the player immediately.
    /// Used by checkpoints to register as the respawn location.
    /// </summary>
    public void SetSpawnPoint(Vector3 position, Quaternion rotation)
    {
        // Only update the spawn point, don't move the player
        spawnPosition = position;
        spawnRotation = rotation;
        
        Debug.Log("Spawn point updated to new checkpoint.");
    }
}