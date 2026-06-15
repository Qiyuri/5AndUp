using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles persistent saving and loading of checkpoint data using PlayerPrefs.
/// Attach to the same GameObject as CheckPoints, or any persistent GameObject.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    // PlayerPrefs keys
    private const string KEY_TRIGGERED_COUNT    = "CP_TriggeredCount";
    private const string KEY_TRIGGERED_PREFIX   = "CP_Triggered_";
    private const string KEY_RESPAWN_ID         = "CP_RespawnID";
    private const string KEY_HAS_SAVE           = "CP_HasSave";

    private static SaveSystem _instance;
    public static SaveSystem Instance => _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // Survive scene loads if needed
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Persist the set of triggered checkpoint IDs and the current respawn checkpoint ID.
    /// Call this whenever a checkpoint is fully activated.
    /// </summary>
    public void SaveCheckpoints(HashSet<int> triggeredCheckpoints, int activeRespawnCheckpointID)
    {
        // Store how many triggered IDs we're saving
        int count = triggeredCheckpoints.Count;
        PlayerPrefs.SetInt(KEY_TRIGGERED_COUNT, count);

        // Store each ID
        int index = 0;
        foreach (int id in triggeredCheckpoints)
        {
            PlayerPrefs.SetString(KEY_TRIGGERED_PREFIX + index, id.ToString());
            index++;
        }

        // Store the current respawn checkpoint ID (-1 means none set)
        PlayerPrefs.SetInt(KEY_RESPAWN_ID, activeRespawnCheckpointID);

        // Mark that a save exists
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);

        PlayerPrefs.Save(); // Flush to disk immediately
        Debug.Log($"[SaveSystem] Saved {count} triggered checkpoint(s). Active respawn ID: {activeRespawnCheckpointID}");
    }

    // ── Load ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if a save file exists.
    /// </summary>
    public bool HasSave() => PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1;

    /// <summary>
    /// Load the set of triggered checkpoint IDs from PlayerPrefs.
    /// </summary>
    public HashSet<int> LoadTriggeredCheckpoints()
    {
        var result = new HashSet<int>();
        int count = PlayerPrefs.GetInt(KEY_TRIGGERED_COUNT, 0);

        for (int i = 0; i < count; i++)
        {
            string raw = PlayerPrefs.GetString(KEY_TRIGGERED_PREFIX + i, "");
            if (int.TryParse(raw, out int id))
                result.Add(id);
        }

        Debug.Log($"[SaveSystem] Loaded {result.Count} triggered checkpoint(s).");
        return result;
    }

    /// <summary>
    /// Load the saved active respawn checkpoint ID.
    /// Returns -1 if none was saved.
    /// </summary>
    public int LoadActiveRespawnCheckpointID()
    {
        return PlayerPrefs.GetInt(KEY_RESPAWN_ID, -1);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Wipe all checkpoint save data. Useful for a "New Game" option.
    /// </summary>
    public void DeleteSave()
    {
        int count = PlayerPrefs.GetInt(KEY_TRIGGERED_COUNT, 0);
        for (int i = 0; i < count; i++)
            PlayerPrefs.DeleteKey(KEY_TRIGGERED_PREFIX + i);

        PlayerPrefs.DeleteKey(KEY_TRIGGERED_COUNT);
        PlayerPrefs.DeleteKey(KEY_RESPAWN_ID);
        PlayerPrefs.DeleteKey(KEY_HAS_SAVE);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] Save data deleted.");
    }
}