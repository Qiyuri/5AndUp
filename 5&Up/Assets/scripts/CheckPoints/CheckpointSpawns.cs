using UnityEngine;

public class CheckpointSpawns : MonoBehaviour
{
    [SerializeField]
    private int checkpointID;

    private CheckPoints checkpointsManager;
    private bool hasBeenActivated = false;

    void Start()
    {
        // Register this checkpoint with the CheckPoints manager
        checkpointsManager = FindAnyObjectByType<CheckPoints>();
        if (checkpointsManager != null)
        {
            checkpointsManager.RegisterCheckpoint(checkpointID, transform.position, transform.rotation);
        }
    }

    public int GetCheckpointID()
    {
        return checkpointID;
    }

    /// <summary>
    /// Detects when the player enters the checkpoint trigger.
    /// Activates this checkpoint as the respawn point (only once).
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to the player and checkpoint hasn't been activated yet
        if (other.CompareTag("Player") && !hasBeenActivated)
        {
            hasBeenActivated = true;
            
            if (checkpointsManager != null)
            {
                // Set this checkpoint as the active respawn point
                checkpointsManager.SetActiveCheckpoint(checkpointID);
                Debug.Log($"Checkpoint {checkpointID} activated! Press F to respawn here.");
            }
            else
            {
                Debug.LogError("CheckPoints manager not found!");
            }
        }
    }
}
