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
    private bool        hasBeenActivated = false;
    private bool        activationPending = false;   // waiting the 2-second delay
    private AudioSource audioSource;
    private Coroutine   activationCoroutine;

    void Start()
    {
        checkpointsManager = FindAnyObjectByType<CheckPoints>();
        if (checkpointsManager != null)
            checkpointsManager.RegisterCheckpoint(checkpointID, transform.position, transform.rotation);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
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

        activationPending    = true;
        activationCoroutine  = StartCoroutine(ActivationSequence());
    }

    // ── Trigger Exit ──────────────────────────────────────────────────────────
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        checkpointsManager?.CancelCheckpointWait();

        // If the player leaves before the delay finishes, cancel the sequence
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
        // Step 1: play the "entering" sound immediately
        if (checkpointSound != null)
            audioSource.PlayOneShot(checkpointSound);

        // Step 2: wait for the delay
        yield return new WaitForSeconds(activationDelay);

        // ── Fully activated from here ─────────────────────────────────────────
        hasBeenActivated  = true;
        activationPending = false;

        // Turn renderers green
        foreach (Renderer r in renderersToTurnGreen)
        {
            if (r != null)
                r.material.color = activatedColour;
        }

        // Play the "activated" sound
        if (checkpointActivatedSound != null)
            audioSource.PlayOneShot(checkpointActivatedSound);

        // Spawn particles at the checkpoint position
        if (particleSystemPrefab != null)
            Instantiate(particleSystemPrefab, transform.position, Quaternion.identity);

        // Enable / disable game objects
        foreach (GameObject go in gameObjectsToEnable)
            if (go != null) go.SetActive(true);

        foreach (GameObject go in gameObjectsToDisable)
            if (go != null) go.SetActive(false);

        Debug.Log($"Checkpoint {checkpointID} activated! Press F to respawn here.");
    }
}