using System.Collections;
using UnityEngine;

public class CheckpointSpawns : MonoBehaviour
{
    [SerializeField]
    private int checkpointID;

    [Header("Checkpoint Activation")]
    [SerializeField] private AudioClip checkpointSound;

    [SerializeField]
    [Tooltip("Afgespeeld na de wachttijd, wanneer het checkpoint volledig activeert.")]
    private AudioClip checkpointActivatedSound;

    [SerializeField]
    [Tooltip("Hoe lang (seconden) het eerste geluid speelt voor het checkpoint activeert.")]
    private float activationDelay = 2f;

    [SerializeField] private ParticleSystem particleSystemPrefab;

    [Header("Visueel – Groen bij activering")]
    [SerializeField]
    [Tooltip("Renderer(s) waarvan de kleur groen wordt als het checkpoint activeert.")]
    private Renderer[] renderersToTurnGreen;

    [SerializeField] private Color activatedColour = Color.green;

    [SerializeField]
    [Tooltip("De standaard kleur van de renderers (rood, of wat ze zijn vóór activering). " +
             "Wordt gebruikt om ze terug te zetten bij een nieuwe run.")]
    private Color defaultColour = Color.red;

    [Header("GameObject wijzigingen")]
    [SerializeField] private GameObject[] gameObjectsToEnable;
    [SerializeField] private GameObject[] gameObjectsToDisable;

    private bool        hasBeenActivated  = false;
    private bool        activationPending = false;
    private AudioSource audioSource;
    private Coroutine   activationCoroutine;

    // ── Statische tellers voor "alle spawns klaar" detectie ──────────────────
    private static int  s_totalSpawns     = 0;
    private static int  s_registeredCount = 0;
    private static bool s_loadTriggered   = false;
    private static bool s_sceneResetDone  = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_totalSpawns     = 0;
        s_registeredCount = 0;
        s_loadTriggered   = false;
        s_sceneResetDone  = false;
    }

    void Awake()
    {
        s_totalSpawns++;

        // Reset visuals meteen in Awake, vóór enige coroutine of frame delay,
        // zodat ze altijd in de juiste staat starten ongeacht wat er vorige run gebeurde.
        ResetVisuals();
        hasBeenActivated  = false;
        activationPending = false;

        if (!s_sceneResetDone)
        {
            s_sceneResetDone = true;
            CheckPoints.Instance?.OnSceneReloaded();
        }
    }

    void OnDestroy()
    {
        s_totalSpawns = Mathf.Max(0, s_totalSpawns - 1);
    }

    void OnEnable()
    {
        CheckPoints.OnRunReset += OnRunReset;
    }

    void OnDisable()
    {
        CheckPoints.OnRunReset -= OnRunReset;
    }

    private void OnRunReset()
    {
        // Nieuwe run gestart — zet visuals terug naar unclaimed staat
        // zodat het checkpoint opnieuw geclaimd kan worden, ook zonder scene reload.
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }
        audioSource?.Stop();
        hasBeenActivated  = false;
        activationPending = false;
        ResetVisuals();
        Debug.Log($"[CheckpointSpawns] Checkpoint {checkpointID}: gereset voor nieuwe run.");
    }

    void Start()
    {
        CheckPoints cp = CheckPoints.Instance;
        if (cp != null)
        {
            cp.RegisterCheckpoint(checkpointID, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError($"[CheckpointSpawns] CheckPoints singleton niet gevonden!");
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        s_registeredCount++;
        if (!s_loadTriggered && s_registeredCount >= s_totalSpawns)
        {
            s_loadTriggered = true;
            cp.OnAllCheckpointsRegistered();
        }

        StartCoroutine(RestoreVisualStateIfNeeded());
    }

    private IEnumerator RestoreVisualStateIfNeeded()
    {
        yield return null;

        CheckPoints cp = CheckPoints.Instance;
        if (cp == null) yield break;

        if (cp.IsCheckpointClaimedThisRun(checkpointID))
        {
            // Al geclaimd in deze run (bijv. scene herlaad mid-run) — herstel visuals.
            ApplyActivatedVisuals(playSounds: false, spawnParticles: false);
            hasBeenActivated = true;
            Debug.Log($"[CheckpointSpawns] Checkpoint {checkpointID}: visueel hersteld (al geclaimd deze run).");
        }
        // Geen else nodig: als het niet geclaimd is, staan de visuals al in de default staat.
    }

    /// <summary>
    /// Resets renderer colours and re-applies the default GameObject active states
    /// so the checkpoint looks unclaimed at the start of every fresh run.
    /// </summary>
    private void ResetVisuals()
    {
        // Gebruik de ingestelde defaultColour (rood) — nooit gecached van het materiaal,
        // zodat dit altijd correct is ongeacht wat er vorige run in memory zat.
        foreach (Renderer r in renderersToTurnGreen)
            if (r != null) r.material.color = defaultColour;

        // Objecten die enabled worden bij activering → weer uitzetten.
        foreach (GameObject go in gameObjectsToEnable)
            if (go != null) go.SetActive(false);

        // Objecten die disabled worden bij activering → weer aanzetten.
        foreach (GameObject go in gameObjectsToDisable)
            if (go != null) go.SetActive(true);
    }

    public int GetCheckpointID() => checkpointID;

    // ── Trigger Enter ─────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CheckPoints cp = CheckPoints.Instance;
        if (cp == null) { Debug.LogError("[CheckpointSpawns] CheckPoints singleton niet gevonden!"); return; }

        cp.SetActiveCheckpoint(checkpointID);

        // RunTimer is NOT called here — it fires inside ActivationSequence
        // after the checkpoint is fully claimed, so respawning through an
        // already-activated checkpoint never resets or splits the timer.

        if (hasBeenActivated || activationPending) return;

        activationPending   = true;
        activationCoroutine = StartCoroutine(ActivationSequence());
    }

    // ── Trigger Exit ──────────────────────────────────────────────────────────
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CheckPoints.Instance?.CancelCheckpointWait();

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

        // Timer fires here — after the checkpoint is fully claimed.
        // Because hasBeenActivated is now true, this can never run twice
        // for the same checkpoint, so respawning won't affect the timer.
        RunTimer.Instance?.OnCheckpointReached(checkpointID);

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