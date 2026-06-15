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
    private SaveSystem saveSystem;

    [SerializeField] private float checkpointWaitTime = 2f; // Time player must stand on checkpoint to obtain it
    private Coroutine activeCheckpointCoroutine;

    // Tracks which checkpoint ID is currently set as the respawn point (-1 = none)
    private int activeRespawnCheckpointID = -1;

    // Lazy reference — re-fetches automatically if the scene changed and the old one is gone
    private RespawnManager _respawnManager;
    private RespawnManager RespawnManager
    {
        get
        {
            if (_respawnManager == null)
                _respawnManager = FindAnyObjectByType<RespawnManager>();
            return _respawnManager;
        }
    }

    void Start()
    {
        saveSystem = FindAnyObjectByType<SaveSystem>();

        if (saveSystem == null)
            Debug.LogWarning("[CheckPoints] SaveSystem not found in scene — progress will not be saved.");
    }

    // Called by CheckpointSpawns objects after they have all registered.
    // We delay load by one frame so every CheckpointSpawns has had its Start() run first.
    private IEnumerator LoadAfterRegistration()
    {
        yield return null; // Wait one frame
        LoadSavedProgress();
    }

    /// <summary>
    /// Called once all CheckpointSpawns have finished registering.
    /// Kicks off the deferred save load.
    /// </summary>
    public void OnAllCheckpointsRegistered()
    {
        StartCoroutine(LoadAfterRegistration());
    }

    // ── Registration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Register a checkpoint with its position and rotation.
    /// Called by CheckpointSpawns objects in the scene.
    /// </summary>
    public void RegisterCheckpoint(int id, Vector3 position, Quaternion rotation)
    {
        Checkpoint checkpoint = new Checkpoint
        {
            id       = id,
            position = position,
            rotation = rotation
        };
        checkpoints[id] = checkpoint;
        Debug.Log($"Checkpoint {id} registered at position {position}");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    /// <summary>
    /// Persist current progress via SaveSystem.
    /// </summary>
    private void SaveProgress()
    {
        if (saveSystem != null)
            saveSystem.SaveCheckpoints(triggeredCheckpoints, activeRespawnCheckpointID);
    }

    /// <summary>
    /// Restore triggered checkpoints and the last respawn point from SaveSystem.
    /// Called one frame after all checkpoints have registered so positions are ready.
    /// </summary>
    private void LoadSavedProgress()
    {
        if (saveSystem == null || !saveSystem.HasSave()) return;

        triggeredCheckpoints = saveSystem.LoadTriggeredCheckpoints();

        int savedRespawnID = saveSystem.LoadActiveRespawnCheckpointID();
        if (savedRespawnID != -1 && checkpoints.ContainsKey(savedRespawnID))
        {
            activeRespawnCheckpointID = savedRespawnID;
            Checkpoint cp = checkpoints[savedRespawnID];
            if (RespawnManager != null)
                RespawnManager.SetSpawnPoint(cp.position, cp.rotation);

            Debug.Log($"[CheckPoints] Restored respawn point to checkpoint {savedRespawnID}");
        }

        Debug.Log($"[CheckPoints] Loaded {triggeredCheckpoints.Count} triggered checkpoint(s) from save.");
    }

    // ── Respawning ────────────────────────────────────────────────────────────

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
            if (RespawnManager != null)
            {
                RespawnManager.RespawnToPosition(checkpoint.position, checkpoint.rotation);
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
            return checkpoints[checkpointID].position;

        Debug.LogError($"Checkpoint {checkpointID} not found!");
        return Vector3.zero;
    }

    // ── Activation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Set the active checkpoint as the respawn point without moving the player immediately.
    /// Player must stand on the checkpoint for checkpointWaitTime seconds.
    /// </summary>
    public void SetActiveCheckpoint(int checkpointID)
    {
        if (checkpoints.ContainsKey(checkpointID))
        {
            if (activeCheckpointCoroutine != null)
                StopCoroutine(activeCheckpointCoroutine);

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
        if (RespawnManager != null)
        {
            RespawnManager.SetSpawnPoint(checkpoint.position, checkpoint.rotation);
            triggeredCheckpoints.Add(checkpointID);
            activeRespawnCheckpointID = checkpointID; // Track which checkpoint is active

            // ── AUTO-SAVE ─────────────────────────────────────────────────────
            SaveProgress();

            Debug.Log($"Checkpoint {checkpointID} obtained after {checkpointWaitTime} seconds");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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