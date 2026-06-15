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
    [Tooltip("Afgespeeld na de wachttijd, wanneer het checkpoint volledig activeert.")]
    private AudioClip checkpointActivatedSound;

    [SerializeField]
    [Tooltip("Hoe lang (seconden) het eerste geluid speelt voor het checkpoint activeert.")]
    private float activationDelay = 2f;

    [SerializeField]
    private ParticleSystem particleSystemPrefab;

    [Header("Visueel – Groen bij activering")]
    [SerializeField]
    [Tooltip("Renderer(s) waarvan de kleur groen wordt als het checkpoint activeert.")]
    private Renderer[] renderersToTurnGreen;

    [SerializeField]
    private Color activatedColour = Color.green;

    [Header("GameObject wijzigingen")]
    [SerializeField]
    private GameObject[] gameObjectsToEnable;

    [SerializeField]
    private GameObject[] gameObjectsToDisable;

    private CheckPoints checkpointsManager;
    private bool        hasBeenActivated  = false;
    private bool        activationPending = false;
    private AudioSource audioSource;
    private Coroutine   activationCoroutine;

    // ── FIX: statische tellers — worden nu betrouwbaar gereset bij elke scene load ──
    private static int  s_totalSpawns     = 0;
    private static int  s_registeredCount = 0;
    private static bool s_loadTriggered   = false;

    /// <summary>
    /// Reset de statische tellers vóór elke scene laadt, zodat het opnieuw werkt
    /// bij het terugkeren naar een scene (bijv. vanuit main menu).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_totalSpawns     = 0;
        s_registeredCount = 0;
        s_loadTriggered   = false;
    }

    void Awake()
    {
        s_totalSpawns++;
    }

    void OnDestroy()
    {
        s_totalSpawns     = Mathf.Max(0, s_totalSpawns - 1);
        // Niet resetten hier — OnDestroy bij scene unload veroorzaakte vals-negatieve resets
    }

    void Start()
    {
        checkpointsManager = FindAnyObjectByType<CheckPoints>();
        if (checkpointsManager != null)
        {
            checkpointsManager.RegisterCheckpoint(checkpointID, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError($"[CheckpointSpawns] CheckPoints manager niet gevonden in scene!");
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Zodra alle spawns Start() hebben gedraaid → zeg het aan de manager
        s_registeredCount++;
        if (!s_loadTriggered && s_registeredCount >= s_totalSpawns)
        {
            s_loadTriggered = true;
            checkpointsManager?.OnAllCheckpointsRegistered();
        }

        // Herstel visuele staat als dit checkpoint al was behaald in een vorige sessie
        StartCoroutine(RestoreVisualStateIfNeeded());
    }

    private IEnumerator RestoreVisualStateIfNeeded()
    {
        yield return null; // Één frame wachten zodat LoadSavedProgress al gelopen heeft

        if (checkpointsManager != null && checkpointsManager.IsCheckpointTriggered(checkpointID))
        {
            ApplyActivatedVisuals(playSounds: false, spawnParticles: false);
            hasBeenActivated = true;
            Debug.Log($"[CheckpointSpawns] Checkpoint {checkpointID}: visueel hersteld vanuit save.");
        }
    }

    public int GetCheckpointID() => checkpointID;

    // ── Trigger Enter ─────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (checkpointsManager == null) { Debug.LogError("[CheckpointSpawns] CheckPoints manager niet gevonden!"); return; }

        checkpointsManager.SetActiveCheckpoint(checkpointID);

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
            Debug.Log($"[CheckpointSpawns] Checkpoint {checkpointID}: speler verliet trigger voor activering.");
        }
    }

    // ── Activeringssequentie ──────────────────────────────────────────────────
    private IEnumerator ActivationSequence()
    {
        if (checkpointSound != null)
            audioSource.PlayOneShot(checkpointSound);

        yield return new WaitForSeconds(activationDelay);

        hasBeenActivated  = true;
        activationPending = false;

        ApplyActivatedVisuals(playSounds: true, spawnParticles: true);

        Debug.Log($"[CheckpointSpawns] Checkpoint {checkpointID} geactiveerd!");
    }

    // ── Gedeelde visuele logica ───────────────────────────────────────────────
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