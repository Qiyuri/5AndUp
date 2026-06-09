using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CheckPoints : MonoBehaviour
{
    [System.Serializable]
    public class Checkpoint
    {
        public int id;
        public Vector3 position;
        public Quaternion rotation;
    }

    private Dictionary<int, Checkpoint> checkpoints = new Dictionary<int, Checkpoint>();
    private HashSet<int> triggeredCheckpoints = new HashSet<int>(); // Tracks which checkpoints have been obtained
    private RespawnManager respawnManager;
    
    [SerializeField] private float checkpointWaitTime = 2f; // Time player must stand on checkpoint to obtain it
    private Coroutine activeCheckpointCoroutine;

    void Start()
    {
        // Get reference to RespawnManager
        respawnManager = FindAnyObjectByType<RespawnManager>();
    }

    /// <summary>
    /// Register a checkpoint with its position and rotation.
    /// Called by CheckpointSpawns objects in the scene.
    /// </summary>
    public void RegisterCheckpoint(int id, Vector3 position, Quaternion rotation)
    {
        Checkpoint checkpoint = new Checkpoint
        {
            id = id,
            position = position,
            rotation = rotation
        };
        checkpoints[id] = checkpoint;
        Debug.Log($"Checkpoint {id} registered at position {position}");
    }

    /// <summary>
    /// Respawn the player to a specific checkpoint.
    /// Call this from button OnClick events.
    /// Only works if the checkpoint has been triggered.
    /// </summary>
    public void RespawnToCheckpoint(int checkpointID)
    {
        if (!triggeredCheckpoints.Contains(checkpointID))
        {
            Debug.LogWarning($"Checkpoint {checkpointID} has not been obtained yet!");
            return;
        }

        if (checkpoints.ContainsKey(checkpointID))
        {
            Checkpoint checkpoint = checkpoints[checkpointID];
            if (respawnManager != null)
            {
                respawnManager.RespawnToPosition(checkpoint.position, checkpoint.rotation);
                Debug.Log($"Respawned to checkpoint {checkpointID}");
            }
            else
            {
                Debug.LogError("RespawnManager not found!");
            }
        }
        else
        {
            Debug.LogError($"Checkpoint {checkpointID} not found!");
        }
    }

    /// <summary>
    /// Get a checkpoint's position by ID.
    /// </summary>
    public Vector3 GetCheckpointPosition(int checkpointID)
    {
        if (checkpoints.ContainsKey(checkpointID))
        {
            return checkpoints[checkpointID].position;
        }
        Debug.LogError($"Checkpoint {checkpointID} not found!");
        return Vector3.zero;
    }

    /// <summary>
    /// Set the active checkpoint as the respawn point without moving the player immediately.
    /// Player must stand on the checkpoint for checkpointWaitTime seconds.
    /// </summary>
    public void SetActiveCheckpoint(int checkpointID)
    {
        if (checkpoints.ContainsKey(checkpointID))
        {
            // Stop any existing checkpoint coroutine
            if (activeCheckpointCoroutine != null)
            {
                StopCoroutine(activeCheckpointCoroutine);
            }
            activeCheckpointCoroutine = StartCoroutine(WaitAndSetCheckpoint(checkpointID));
        }
        else
        {
            Debug.LogError($"Checkpoint {checkpointID} not found!");
        }
    }

    /// <summary>
    /// Coroutine that waits for the specified time before setting the checkpoint.
    /// </summary>
    private IEnumerator WaitAndSetCheckpoint(int checkpointID)
    {
        yield return new WaitForSeconds(checkpointWaitTime);
        
        Checkpoint checkpoint = checkpoints[checkpointID];
        if (respawnManager != null)
        {
            respawnManager.SetSpawnPoint(checkpoint.position, checkpoint.rotation);
            triggeredCheckpoints.Add(checkpointID); // Mark checkpoint as obtained
            Debug.Log($"Checkpoint {checkpointID} obtained after {checkpointWaitTime} seconds");
        }
    }

    /// <summary>
    /// Check if a checkpoint has been triggered/obtained.
    /// </summary>
    public bool IsCheckpointTriggered(int checkpointID)
    {
        return triggeredCheckpoints.Contains(checkpointID);
    }

    /// <summary>
    /// Cancel the checkpoint wait coroutine (called when player leaves the trigger).
    /// </summary>
    public void CancelCheckpointWait()
    {
        if (activeCheckpointCoroutine != null)
        {
            StopCoroutine(activeCheckpointCoroutine);
            activeCheckpointCoroutine = null;
            Debug.Log("Checkpoint wait cancelled - player left the trigger");
        }
    }
}
