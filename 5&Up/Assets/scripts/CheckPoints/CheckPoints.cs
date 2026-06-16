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

    private Dictionary<int, Checkpoint> checkpoints = new Dictionary<int, Checkpoint>();
    private HashSet<int> triggeredCheckpoints = new HashSet<int>();
    private SaveSystem saveSystem;

    [SerializeField] private float checkpointWaitTime = 2f;
    private Coroutine activeCheckpointCoroutine;

    private int activeRespawnCheckpointID = -1;

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

    void Awake()
    {
        // Singleton: vernietig duplicaten, overleef scene loads
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        saveSystem = FindAnyObjectByType<SaveSystem>();
        if (saveSystem == null)
            Debug.LogWarning("[CheckPoints] SaveSystem niet gevonden — voortgang wordt niet opgeslagen.");
    }

    // Elke keer dat een nieuwe scene laadt, moeten de checkpoint-posities opnieuw
    // geregistreerd worden (de wereld is nieuw), maar de triggered-set blijft bewaard.
    // Reset daarom alleen de positie-dictionary, niet de voortgang.
    public void OnSceneReloaded()
    {
        checkpoints.Clear();
        _respawnManager = null; // Haal de RespawnManager opnieuw op in de nieuwe scene
        Debug.Log("[CheckPoints] Scene herladen — checkpoint-posities gereset, voortgang bewaard.");
    }

    private IEnumerator LoadAfterRegistration()
    {
        yield return null;
        LoadSavedProgress();
    }

    public void OnAllCheckpointsRegistered()
    {
        StartCoroutine(LoadAfterRegistration());
    }

    // ── Registration ──────────────────────────────────────────────────────────

    public void RegisterCheckpoint(int id, Vector3 position, Quaternion rotation)
    {
        checkpoints[id] = new Checkpoint { id = id, position = position, rotation = rotation };
        Debug.Log($"[CheckPoints] Checkpoint {id} geregistreerd op {position}");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    private void SaveProgress()
    {
        // SaveSystem is ook DontDestroyOnLoad, dus altijd beschikbaar
        if (saveSystem == null)
            saveSystem = FindAnyObjectByType<SaveSystem>();

        if (saveSystem != null)
            saveSystem.SaveCheckpoints(triggeredCheckpoints, activeRespawnCheckpointID);
        else
            Debug.LogWarning("[CheckPoints] Kan niet opslaan: SaveSystem is null.");
    }

    private void LoadSavedProgress()
    {
        if (saveSystem == null)
            saveSystem = FindAnyObjectByType<SaveSystem>();

        if (saveSystem == null)
        {
            Debug.LogWarning("[CheckPoints] LoadSavedProgress: SaveSystem is null, niets geladen.");
            return;
        }

        if (!saveSystem.HasSave())
        {
            Debug.Log("[CheckPoints] Geen opgeslagen voortgang gevonden.");
            return;
        }

        triggeredCheckpoints = saveSystem.LoadTriggeredCheckpoints();

        int savedRespawnID = saveSystem.LoadActiveRespawnCheckpointID();
        if (savedRespawnID != -1 && checkpoints.ContainsKey(savedRespawnID))
        {
            activeRespawnCheckpointID = savedRespawnID;
            Checkpoint cp = checkpoints[savedRespawnID];
            if (RespawnManager != null)
                RespawnManager.SetSpawnPoint(cp.position, cp.rotation);

            Debug.Log($"[CheckPoints] Respawn hersteld naar checkpoint {savedRespawnID}");
        }

        Debug.Log($"[CheckPoints] {triggeredCheckpoints.Count} checkpoint(s) geladen uit save.");
    }

    // ── Respawning ────────────────────────────────────────────────────────────

    public void RespawnToCheckpoint(int checkpointID)
    {
        if (!triggeredCheckpoints.Contains(checkpointID))
        {
            Debug.LogWarning($"[CheckPoints] Checkpoint {checkpointID} is nog niet behaald!");
            return;
        }

        if (checkpoints.ContainsKey(checkpointID))
        {
            Checkpoint checkpoint = checkpoints[checkpointID];
            if (RespawnManager != null)
            {
                RespawnManager.RespawnToPosition(checkpoint.position, checkpoint.rotation);
                Debug.Log($"[CheckPoints] Gespawnd naar checkpoint {checkpointID}");
            }
            else
            {
                Debug.LogError("[CheckPoints] RespawnManager niet gevonden!");
            }
        }
        else
        {
            Debug.LogError($"[CheckPoints] Checkpoint {checkpointID} bestaat niet!");
        }
    }

    public Vector3 GetCheckpointPosition(int checkpointID)
    {
        if (checkpoints.ContainsKey(checkpointID))
            return checkpoints[checkpointID].position;

        Debug.LogError($"[CheckPoints] Checkpoint {checkpointID} bestaat niet!");
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
            Debug.LogError($"[CheckPoints] Checkpoint {checkpointID} bestaat niet!");
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

            Debug.Log($"[CheckPoints] Checkpoint {checkpointID} behaald na {checkpointWaitTime} seconden.");
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
            Debug.Log("[CheckPoints] Checkpoint wacht geannuleerd — speler heeft trigger verlaten.");
        }
    }
}