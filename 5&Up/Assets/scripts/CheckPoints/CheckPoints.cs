using UnityEngine;
using UnityEngine.UI;
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

    [Header("Cheat Image")]
    [SerializeField]
    [Tooltip("Naam van het cheese GameObject dat getoond wordt na de cheat.")]
    private string cheatImageName = "cheese";
    private const float CHEAT_IMAGE_DURATION = 5f;
    private Coroutine   _cheatImageCoroutine;

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

    // ── Cheat code state ──────────────────────────────────────────────────────
    private const string CHEAT_SEQUENCE = "frans maakt veel kans";
    private const int    CHEAT_REPEATS  = 1;
    private const float  CHEAT_WINDOW        = 10f;
    private const float  CHEAT_MAX_WAIT      = 3f;
    private string       cheatBuffer    = "";
    private List<float>  cheatTimes     = new List<float>();

    // ── Awake ─────────────────────────────────────────────────────────────────

    void Awake()
    {
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

    void Update()
    {
        DetectCheatCode();
    }

    // ── Scene reload ──────────────────────────────────────────────────────────

    public void OnSceneReloaded()
    {
        checkpoints.Clear();
        _respawnManager = null;
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

    // ── Cheat code: 3x "1234567890-=" binnen 10 seconden ────────────────────

    private void DetectCheatCode()
    {
        if (string.IsNullOrEmpty(Input.inputString)) return;

        foreach (char c in Input.inputString)
        {
            cheatBuffer += c;

            // Buffer bijknippen zodat hij nooit meer dan 3x de sequence lang is
            int maxLen = CHEAT_SEQUENCE.Length * CHEAT_REPEATS;
            if (cheatBuffer.Length > maxLen)
                cheatBuffer = cheatBuffer.Substring(cheatBuffer.Length - maxLen);

            // Kijk of de sequence net afgerond is
            if (cheatBuffer.EndsWith(CHEAT_SEQUENCE))
            {
                // Verwijder tijden buiten het tijdvenster
                cheatTimes.RemoveAll(t => Time.time - t > CHEAT_WINDOW);
                cheatTimes.Add(Time.time);

                Debug.Log($"[CheckPoints] Cheat sequence ingevoerd ({cheatTimes.Count}/{CHEAT_REPEATS}).");

                if (cheatTimes.Count >= CHEAT_REPEATS)
                {
                    UnlockAllCheckpoints();
                    cheatBuffer = "";
                    cheatTimes.Clear();
                }
            }
        }
    }

    private void UnlockAllCheckpoints()
    {
        // Stop eventuele vorige cheat-run
        if (_cheatCoroutine != null)
            StopCoroutine(_cheatCoroutine);

        _cheatCoroutine = StartCoroutine(CheatUnlockSequence());
    }

    private Coroutine _cheatCoroutine;

    private IEnumerator CheatUnlockSequence()
    {
        if (RespawnManager == null)
        {
            Debug.LogError("[CheckPoints] CHEAT: RespawnManager niet gevonden, afgebroken.");
            yield break;
        }

        // Onthoud de startpositie van de speler zodat we terug kunnen keren
        Vector3    returnPosition = RespawnManager.GetCurrentPosition();
        Quaternion returnRotation = RespawnManager.GetCurrentRotation();

        // Sorteer checkpoints op ID zodat we ze op volgorde aflopen
        List<int> sortedIDs = new List<int>(checkpoints.Keys);
        sortedIDs.Sort();

        Debug.Log($"[CheckPoints] CHEAT GESTART: {sortedIDs.Count} checkpoints worden ontgrendeld.");

        foreach (int id in sortedIDs)
        {
            // Sla over als dit checkpoint al ontgrendeld is
            if (triggeredCheckpoints.Contains(id))
            {
                Debug.Log($"[CheckPoints] CHEAT: Checkpoint {id} al ontgrendeld, sla over.");
                continue;
            }

            Checkpoint cp = checkpoints[id];

            // Teleporteer de speler naar dit checkpoint
            RespawnManager.RespawnToPosition(cp.position, cp.rotation);
            Debug.Log($"[CheckPoints] CHEAT: Geteleporteerd naar checkpoint {id}, wacht max {CHEAT_MAX_WAIT}s of tot geclaimd...");

            // Wacht maximaal CHEAT_MAX_WAIT seconden, of tot het checkpoint
            // al geclaimd is (bijv. doordat de trigger zelf ook afging).
            float waited = 0f;
            while (waited < CHEAT_MAX_WAIT && !triggeredCheckpoints.Contains(id))
            {
                yield return new WaitForFixedUpdate();
                waited += Time.fixedDeltaTime;
            }

            // Unlock dit checkpoint als het nog niet geclaimd is
            if (!triggeredCheckpoints.Contains(id))
            {
                triggeredCheckpoints.Add(id);
                activeRespawnCheckpointID = id;
                RespawnManager.SetSpawnPoint(cp.position, cp.rotation);
                SaveProgress();
            }

            Debug.Log($"[CheckPoints] CHEAT: Checkpoint {id} ontgrendeld na {waited:F1}s.");
        }

        // Teleporteer de speler terug naar waar hij was
        RespawnManager.RespawnToPosition(returnPosition, returnRotation);
        Debug.Log($"[CheckPoints] CHEAT KLAAR: alle checkpoints ontgrendeld, speler terug op startpositie.");

        // Zoek het cheese object via alle Transforms in de scene
        // (ook inactive objecten worden zo gevonden)
        GameObject cheatGO = null;
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.gameObject.scene.isLoaded && t.gameObject.name == cheatImageName)
            {
                cheatGO = t.gameObject;
                break;
            }
        }

        if (cheatGO != null)
        {
            if (_cheatImageCoroutine != null)
                StopCoroutine(_cheatImageCoroutine);
            _cheatImageCoroutine = StartCoroutine(ShowCheatCanvas(cheatGO));
        }
        else
        {
            Debug.LogWarning($"[CheckPoints] Cheat object '{cheatImageName}' niet gevonden in de scene!");
        }

        _cheatCoroutine = null;
    }

    private IEnumerator ShowCheatCanvas(GameObject cheatGO)
    {
        cheatGO.SetActive(true);
        yield return new WaitForSeconds(CHEAT_IMAGE_DURATION);
        cheatGO.SetActive(false);
        _cheatImageCoroutine = null;
    }
}