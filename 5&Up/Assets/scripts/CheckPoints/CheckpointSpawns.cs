using UnityEngine;

public class CheckpointSpawns : MonoBehaviour
{
    [SerializeField]
    private int checkpointID;

    [Header("Checkpoint Activation")]
    [SerializeField]
    private AudioClip checkpointSound;

    [SerializeField]
    private ParticleSystem particleSystemPrefab;

    private CheckPoints checkpointsManager;
    private bool hasBeenActivated = false;
    private AudioSource audioSource;

    void Start()
    {
        // Register this checkpoint with the CheckPoints manager
        checkpointsManager = FindAnyObjectByType<CheckPoints>();
        if (checkpointsManager != null)
        {
            checkpointsManager.RegisterCheckpoint(checkpointID, transform.position, transform.rotation);
        }

        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public int GetCheckpointID()
    {
        return checkpointID;
    }

    /// <summary>
    /// Detects when the player enters the checkpoint trigger.
    /// Activates this checkpoint as the respawn point (only once).
    /// Plays a sound and spawns particles at the player's position.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to the player
        if (other.CompareTag("Player"))
        {
            if (checkpointsManager != null)
            {
                // Set this checkpoint as the active respawn point
                checkpointsManager.SetActiveCheckpoint(checkpointID);
                
                // Play sound only once
                if (!hasBeenActivated && checkpointSound != null)
                {
                    audioSource.PlayOneShot(checkpointSound);
                }

                // Spawn particle system at checkpoint's position
                if (!hasBeenActivated && particleSystemPrefab != null)
                {
                    Instantiate(particleSystemPrefab, transform.position, Quaternion.identity);
                }

                // Mark as activated if this is the first time
                if (!hasBeenActivated)
                {
                    hasBeenActivated = true;
                    Debug.Log($"Checkpoint {checkpointID} activated! Press F to respawn here.");
                }
            }
            else
            {
                Debug.LogError("CheckPoints manager not found!");
            }
        }
    }
}
