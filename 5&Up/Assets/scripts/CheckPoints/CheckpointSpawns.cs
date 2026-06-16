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
    private static bool s_sceneResetDone  = false; // Voorkomt dubbele OnSceneReloaded aanroep

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

        // Eerste spawn in deze scene reset de positie-dictionary in de singleton
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

    void Start()
    {
        // Gebruik de singleton i.p.v. FindAnyObjectByType — werkt ook na scene reload
        CheckPoints cp = CheckPoints.Instance;
        if (cp != null)
        {
            cp.RegisterCheckpoint(checkpointID, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError($"[CheckpointSpawns] CheckPoints singleton niet gevonden! Zorg dat CheckPoints.cs DontDestroyOnLoad heeft.");
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Zodra alle spawns Start() hebben gedraaid → laad save
        s_registeredCount++;
        if (!s_loadTriggered && s_registeredCount >= s_totalSpawns)
        {
            s_loadTriggered = true;
            cp.OnAllCheckpointsRegistered();
        }

        // Herstel visuele staat als checkpoint al was behaald in vorige sessie
        StartCoroutine(RestoreVisualStateIfNeeded());
    }

    private IEnumerator RestoreVisualStateIfNeeded()
    {
        yield return null; // Wacht tot LoadSavedProgress klaar is

        if (CheckPoints.Instance != null && CheckPoints.Instance.IsCheckpointTriggered(checkpointID))
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

        CheckPoints cp = CheckPoints.Instance;
        if (cp == null) { Debug.LogError("[CheckpointSpawns] CheckPoints singleton niet gevonden!"); return; }

        cp.SetActiveCheckpoint(checkpointID);

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