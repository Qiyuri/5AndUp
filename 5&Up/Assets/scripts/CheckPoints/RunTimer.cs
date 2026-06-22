// RunTimer.cs
// Timer starts (or restarts) whenever the start line is crossed.
// Splits on each subsequent checkpoint claim, stops at the end zone.
// PB saved via PlayerPrefs.

using UnityEngine;
using TMPro;
using System.Collections;

public class RunTimer : MonoBehaviour
{
    private static RunTimer _instance;
    public static RunTimer Instance => _instance;

    [Header("Settings")]
    [Tooltip("ID of the checkpoint that starts the timer (usually 0).")]
    [SerializeField] private int startCheckpointID = 0;
    [Tooltip("How long sector info stays on screen (seconds).")]
    [SerializeField] private float infoHoldTime = 3f;

    [Header("UI")]
    [Tooltip("Always visible. Shows the running total time.")]
    [SerializeField] private TextMeshProUGUI totalTimeText;
    [Tooltip("Shows sector time + delta on each checkpoint, final time at finish.")]
    [SerializeField] private TextMeshProUGUI infoText;

    // ── State ──────────────────────────────────────────────────────────────────
    private bool  _running;
    private bool  _finished;          // true between finish and next start-line cross
    private float _startTime;
    private float _lastSplitTime;
    private int   _lastHitID;

    private const int MAX = 32;
    private float[] _sectorTimes = new float[MAX];
    private float   _pbTotal;
    private float[] _pbSectors   = new float[MAX];

    private Coroutine _infoRoutine;

    // ── Awake ──────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        LoadPB();
        if (infoText)      infoText.text     = "";
        if (totalTimeText) totalTimeText.text = "00:00.000";
    }

    void OnDestroy() { if (_instance == this) _instance = null; }

    // ── Update ─────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!_running) return;
        if (totalTimeText)
            totalTimeText.text = Fmt(Time.time - _startTime);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called directly by StartLineTrigger every time the player crosses the
    /// start line — bypasses the one-shot checkpoint activation system so the
    /// run always resets, even after a finished run.
    /// </summary>
    public void OnStartLineCrossed()
    {
        StartRun();
    }

    /// <summary>
    /// Called from CheckpointSpawns.ActivationSequence() AFTER a checkpoint
    /// is fully claimed. Checkpoint 0 is handled by OnStartLineCrossed instead.
    /// All others record a split (ignored if timer isn't running or ID is
    /// out of order).
    /// </summary>
    public void OnCheckpointReached(int id)
    {
        // CP0 is now handled by the start-line trigger; ignore it here.
        if (id == startCheckpointID) return;

        if (!_running || id <= _lastHitID) return;

        float elapsed    = Time.time - _startTime;
        float sectorTime = elapsed - _lastSplitTime;

        _sectorTimes[id] = sectorTime;
        _lastSplitTime   = elapsed;
        _lastHitID       = id;

        float pb    = _pbSectors[id];
        bool  hasPB = pb > 0f;
        float delta = hasPB ? sectorTime - pb : 0f;

        string deltaStr = hasPB ? $"  ({delta:+0.000;-0.000}s)" : "  (first)";
        ShowInfo($"S{id}  {Fmt(sectorTime)}{deltaStr}");
    }

    /// <summary>Called from EndZoneTrigger.cs.</summary>
    public void OnRunFinished()
    {
        if (!_running) return;

        _running  = false;
        _finished = true;

        float total   = Time.time - _startTime;
        bool  isNewPB = _pbTotal <= 0f || total < _pbTotal;

        if (isNewPB)
        {
            _pbTotal = total;
            for (int i = 0; i < MAX; i++)
                if (_sectorTimes[i] > 0f) _pbSectors[i] = _sectorTimes[i];
            SavePB();
        }

        string pbLine = isNewPB
            ? "new PB!"
            : $"{total - _pbTotal:+0.000;-0.000}s vs PB ({Fmt(_pbTotal)})";

        // Freeze the displayed time at the finish value.
        if (totalTimeText) totalTimeText.text = Fmt(total);

        // Show the result permanently until the next run starts.
        ShowInfo(pbLine, permanent: true);

        Debug.Log($"[RunTimer] Finish — {Fmt(total)}  {pbLine}");
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private void StartRun()
    {
        _running       = true;
        _finished      = false;
        _startTime     = Time.time;
        _lastSplitTime = 0f;
        _lastHitID     = startCheckpointID;
        for (int i = 0; i < MAX; i++) _sectorTimes[i] = 0f;

        // Reset which checkpoints were claimed this run so their visuals,
        // sounds, and particles can fire again. Permanent save is untouched.
        CheckPoints.Instance?.ResetRunState();

        // Clear any finish message / sector info immediately.
        ShowInfo("", permanent: true);
        if (totalTimeText) totalTimeText.text = "00:00.000";

        Debug.Log("[RunTimer] Timer started / reset.");
    }

    private void ShowInfo(string msg, bool permanent = false)
    {
        if (_infoRoutine != null) StopCoroutine(_infoRoutine);
        if (permanent) { if (infoText) infoText.text = msg; return; }
        _infoRoutine = StartCoroutine(InfoRoutine(msg));
    }

    private IEnumerator InfoRoutine(string msg)
    {
        if (infoText) infoText.text = msg;
        yield return new WaitForSeconds(infoHoldTime);
        if (infoText) infoText.text = "";
    }

    // ── Save / Load ────────────────────────────────────────────────────────────

    private void SavePB()
    {
        PlayerPrefs.SetFloat("RT_Total", _pbTotal);
        for (int i = 0; i < MAX; i++)
            PlayerPrefs.SetFloat("RT_S" + i, _pbSectors[i]);
        PlayerPrefs.Save();
        Debug.Log($"[RunTimer] PB saved: {Fmt(_pbTotal)}");
    }

    private void LoadPB()
    {
        _pbTotal = PlayerPrefs.GetFloat("RT_Total", 0f);
        for (int i = 0; i < MAX; i++)
            _pbSectors[i] = PlayerPrefs.GetFloat("RT_S" + i, 0f);
        if (_pbTotal > 0f) Debug.Log($"[RunTimer] PB loaded: {Fmt(_pbTotal)}");
    }

    private string Fmt(float s)
    {
        int m  = (int)(s / 60f);
        int se = (int)(s % 60f);
        int ms = (int)((s * 1000f) % 1000f);
        return $"{m:00}:{se:00}.{ms:000}";
    }
}