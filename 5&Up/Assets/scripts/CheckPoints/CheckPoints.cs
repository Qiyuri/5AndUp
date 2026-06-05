using UnityEngine;
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
    private RespawnManager respawnManager;

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
    /// </summary>
    public void RespawnToCheckpoint(int checkpointID)
    {
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
    /// </summary>
    public void SetActiveCheckpoint(int checkpointID)
    {
        if (checkpoints.ContainsKey(checkpointID))
        {
            Checkpoint checkpoint = checkpoints[checkpointID];
            if (respawnManager != null)
            {
                // Only update the spawn point, don't respawn yet
                respawnManager.SetSpawnPoint(checkpoint.position, checkpoint.rotation);
            }
        }
        else
        {
            Debug.LogError($"Checkpoint {checkpointID} not found!");
        }
    }
}
