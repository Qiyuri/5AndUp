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

    // ── Singleton ─────────────────────────────────────────────────────────────

    private static CheckPoints _instance;
    public static CheckPoints Instance => _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── State ─────────────────────────────────────────────────────────────────

    // Cleared on every scene load so CheckpointSpawns objects can re-register
    private Dictionary<int, Checkpoint> checkpoints = new Dictionary<int, Checkpoint>();
    private HashSet<int> triggeredCheckpoints = new HashSet<int>();
    private SaveSystem saveSystem;

    [SerializeField] private float checkpointWaitTime = 2f;
    private Coroutine activeCheckpointCoroutine;

    private int activeRespawnCheckpointID = -1;

    // Lazy reference — re-fetches automatically after every scene load
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

    // ── Scene reload support ──────────────────────────────────────────────────

    /// <summary>
    /// Called by CheckpointSpawns in OnEnable (before Start) so the persistent
    /// singleton knows a new scene is loading and clears stale position data.
    /// triggeredCheckpoints is intentionally kept — they survive across scenes.
    /// </summary>
    public void OnSceneReloading()
    {
        checkpoints.Clear();
        _respawnManager = null; // Force re-fetch after scene load

        if (activeCheckpointCoroutine != null)
        {
            StopCoroutine(activeCheckpointCoroutine);
            activeCheckpointCoroutine = null;
        }

        Debug.Log("[CheckPoints] Scene reloading — checkpoint positions cleared for re-registration.");
    }

    // ── Registration ──────────────────────────────────────────────────────────

    public void RegisterCheckpoint(int id, Vector3 position, Quaternion rotation)
    {
        checkpoints[id] = new Checkpoint { id = id, position = position, rotation = rotation };
        Debug.Log($"[CheckPoints] Checkpoint {id} registered at {position}");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    // Called by CheckpointSpawns once all instances have finished registering.
    public void OnAllCheckpointsRegistered()
    {
        StartCoroutine(LoadAfterRegistration());
    }

    private IEnumerator LoadAfterRegistration()
    {
        yield return null;
        LoadSavedProgress();
    }

    private void SaveProgress()
    {
        if (saveSystem != null)
            saveSystem.SaveCheckpoints(triggeredCheckpoints, activeRespawnCheckpointID);
    }

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

    public void RespawnToCheckpoint(int checkpointID)
    {
        if (!triggeredCheckpoints.Contains(checkpointID))
        {
            Debug.LogWarning($"[CheckPoints] Checkpoint {checkpointID} has not been obtained yet!");
            return;
        }

        if (checkpoints.ContainsKey(checkpointID))
        {
            Checkpoint checkpoint = checkpoints[checkpointID];
            if (RespawnManager != null)
            {
                RespawnManager.RespawnToPosition(checkpoint.position, checkpoint.rotation);
                Debug.Log($"[CheckPoints] Respawned to checkpoint {checkpointID}");
            }
            else
            {
                Debug.LogError("[CheckPoints] RespawnManager not found!");
            }
        }
        else
        {
            Debug.LogError($"[CheckPoints] Checkpoint {checkpointID} not found!");
        }
    }

    public Vector3 GetCheckpointPosition(int checkpointID)
    {
        if (checkpoints.ContainsKey(checkpointID))
            return checkpoints[checkpointID].position;

        Debug.LogError($"[CheckPoints] Checkpoint {checkpointID} not found!");
        return Vector3.zero;
    }

    // ── Activation ────────────────────────────────────────────────────────────

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
            Debug.LogError($"[CheckPoints] Checkpoint {checkpointID} not found!");
        }
    }

    private IEnumerator WaitAndSetCheckpoint(int checkpointID)
    {
        yield return new WaitForSeconds(checkpointWaitTime);

        Checkpoint checkpoint = checkpoints[checkpointID];
        if (RespawnManager != null)
        {
            RespawnManager.SetSpawnPoint(checkpoint.position, checkpoint.rotation);
            triggeredCheckpoints.Add(checkpointID);
            activeRespawnCheckpointID = checkpointID;
            SaveProgress();
            Debug.Log($"[CheckPoints] Checkpoint {checkpointID} obtained after {checkpointWaitTime}s");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public bool IsCheckpointTriggered(int checkpointID)
    {
        return triggeredCheckpoints.Contains(checkpointID);
    }

    public void CancelCheckpointWait()
    {
        if (activeCheckpointCoroutine != null)
        {
            StopCoroutine(activeCheckpointCoroutine);
            activeCheckpointCoroutine = null;
            Debug.Log("[CheckPoints] Checkpoint wait cancelled — player left the trigger.");
        }
    }
}