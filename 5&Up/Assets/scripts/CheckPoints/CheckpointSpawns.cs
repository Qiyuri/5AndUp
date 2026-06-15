using System.Collections;
using UnityEngine;

public class CheckpointSpawns : MonoBehaviour
{
    [SerializeField]
    private int checkpointID;

    [Header("Checkpoint Activation")]
    [SerializeField]
    private AudioClip checkpointSound;

    [SerializeField]
    [Tooltip("Played after the wait time, when the checkpoint fully activates.")]
    private AudioClip checkpointActivatedSound;

    [SerializeField]
    [Tooltip("How long (seconds) the first sound plays before the checkpoint activates.")]
    private float activationDelay = 2f;

    [SerializeField]
    private ParticleSystem particleSystemPrefab;

    [Header("Visual – Green on Activation")]
    [SerializeField]
    [Tooltip("Renderer(s) whose material colour will change to green when the checkpoint activates.")]
    private Renderer[] renderersToTurnGreen;

    [SerializeField]
    private Color activatedColour = Color.green;

    [Header("GameObject Changes")]
    [SerializeField]
    private GameObject[] gameObjectsToEnable;

    [SerializeField]
    private GameObject[] gameObjectsToDisable;

    private CheckPoints checkpointsManager;
    private bool        hasBeenActivated  = false;
    private bool        activationPending = false;
    private AudioSource audioSource;
    private Coroutine   activationCoroutine;

    // Tracks how many CheckpointSpawns instances exist so we know when all have registered
    private static int  s_totalSpawns      = 0;
    private static int  s_registeredCount  = 0;
    private static bool s_loadTriggered    = false; // Only fire OnAllCheckpointsRegistered once per scene load

    void Awake()
    {
        s_totalSpawns++;
    }

    void OnDestroy()
    {
        // Keep counts consistent if objects are destroyed before Start
        s_totalSpawns    = Mathf.Max(0, s_totalSpawns - 1);
        s_loadTriggered  = false;
        s_registeredCount = 0;
    }

    void Start()
    {
        checkpointsManager = FindAnyObjectByType<CheckPoints>();
        if (checkpointsManager != null)
        {
            checkpointsManager.RegisterCheckpoint(checkpointID, transform.position, transform.rotation);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Track how many spawns have finished Start(); when all are done, tell the manager to load.
        s_registeredCount++;
        if (!s_loadTriggered && s_registeredCount >= s_totalSpawns)
        {
            s_loadTriggered = true;
            checkpointsManager?.OnAllCheckpointsRegistered();
        }

        // Restore visual state for checkpoints that were already activated in a previous session.
        // We wait one frame so that OnAllCheckpointsRegistered / LoadSavedProgress has run first.
        StartCoroutine(RestoreVisualStateIfNeeded());
    }

    /// <summary>
    /// Wait one frame so the save data has been loaded, then restore visuals if this
    /// checkpoint was already obtained in a previous session.
    /// </summary>
    private IEnumerator RestoreVisualStateIfNeeded()
    {
        yield return null; // One frame — LoadSavedProgress has now run

        if (checkpointsManager != null && checkpointsManager.IsCheckpointTriggered(checkpointID))
        {
            ApplyActivatedVisuals(playSounds: false, spawnParticles: false);
            hasBeenActivated = true;
            Debug.Log($"Checkpoint {checkpointID}: restored activated visuals from save.");
        }
    }

    public int GetCheckpointID() => checkpointID;

    // ── Trigger Enter ─────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (checkpointsManager == null) { Debug.LogError("CheckPoints manager not found!"); return; }

        // Always update the active respawn point (even on re-entry after activation)
        checkpointsManager.SetActiveCheckpoint(checkpointID);

        // Only run the activation sequence once
        if (hasBeenActivated || activationPending) return;

        activationPending   = true;
        activationCoroutine = StartCoroutine(ActivationSequence());
    }

    // ── Trigger Exit ──────────────────────────────────────────────────────────
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        checkpointsManager?.CancelCheckpointWait();

        if (activationPending && !hasBeenActivated)
        {
            if (activationCoroutine != null)
                StopCoroutine(activationCoroutine);

            activationPending = false;
            audioSource.Stop();
            Debug.Log($"Checkpoint {checkpointID}: player left before activation completed.");
        }
    }

    // ── Activation sequence ───────────────────────────────────────────────────
    private IEnumerator ActivationSequence()
    {
        if (checkpointSound != null)
            audioSource.PlayOneShot(checkpointSound);

        yield return new WaitForSeconds(activationDelay);

        hasBeenActivated  = true;
        activationPending = false;

        ApplyActivatedVisuals(playSounds: true, spawnParticles: true);

        Debug.Log($"Checkpoint {checkpointID} activated! Press F to respawn here.");
    }

    // ── Shared visual logic ───────────────────────────────────────────────────

    /// <summary>
    /// Apply the fully-activated visual state.
    /// playAudio and spawnParticles are false when restoring from a save (already seen/heard before).
    /// </summary>
    private void ApplyActivatedVisuals(bool playSounds, bool spawnParticles)
    {
        foreach (Renderer r in renderersToTurnGreen)
            if (r != null) r.material.color = activatedColour;

        if (playSounds && checkpointActivatedSound != null)
            audioSource.PlayOneShot(checkpointActivatedSound);

        if (spawnParticles && particleSystemPrefab != null)
            Instantiate(particleSystemPrefab, transform.position, Quaternion.identity);

        foreach (GameObject go in gameObjectsToEnable)
            if (go != null) go.SetActive(true);

        foreach (GameObject go in gameObjectsToDisable)
            if (go != null) go.SetActive(false);
    }
}