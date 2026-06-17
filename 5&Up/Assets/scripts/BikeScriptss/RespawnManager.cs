using System.Collections;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Time required to hold F for respawn.")]
    public float respawnHoldTime = 1f;

    [Header("Audio")]
    [Tooltip("Sound to play when respawning.")]
    public AudioClip respawnSound;

    private Vector3     spawnPosition;
    private Quaternion  spawnRotation;
    private float       holdTimer = 0f;
    private AudioSource audioSource;
    private Rigidbody   rb;

    void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= respawnHoldTime)
            {
                Respawn();
                holdTimer = 0f;
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    void Respawn()
    {
        RespawnToPosition(spawnPosition, spawnRotation);
    }

    /// <summary>
    /// Respawn to a specific position and rotation.
    /// </summary>
    public void RespawnToPosition(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;

        if (audioSource != null && respawnSound != null)
            audioSource.PlayOneShot(respawnSound);

        StartCoroutine(DoRespawn(position, rotation));
    }

    /// <summary>
    /// Set the spawn point without moving the player immediately.
    /// </summary>
    public void SetSpawnPoint(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;

        Debug.Log("Spawn point updated to new checkpoint.");
    }

    /// <summary>
    /// Geeft de huidige spawnpositie terug — gebruikt door de cheat in CheckPoints.
    /// </summary>
    public Vector3 GetCurrentPosition() => spawnPosition;

    /// <summary>
    /// Geeft de huidige spawnrotatie terug — gebruikt door de cheat in CheckPoints.
    /// </summary>
    public Quaternion GetCurrentRotation() => spawnRotation;

    private IEnumerator DoRespawn(Vector3 position, Quaternion rotation)
    {
        TestMovement moveScript = GetComponent<TestMovement>();
        if (moveScript != null)
            moveScript.ResetSpeeds();

        if (rb != null)
        {
            bool wasKinematic  = rb.isKinematic;
            rb.isKinematic     = true;
            rb.position        = position;
            rb.rotation        = rotation;

            yield return new WaitForFixedUpdate();

            rb.isKinematic     = wasKinematic;
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}