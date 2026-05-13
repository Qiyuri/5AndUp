using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Time required to hold F for respawn.")]
    public float respawnHoldTime = 1f;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float holdTimer = 0f;

    void Start()
    {
        // Store the initial position and rotation as spawn point
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    void Update()
    {
        // Check if F is held
        if (Input.GetKey(KeyCode.F))
        {
            holdTimer += Time.deltaTime;

            // If held long enough, respawn
            if (holdTimer >= respawnHoldTime)
            {
                Respawn();
                holdTimer = 0f;
            }
        }
        else
        {
            // Reset timer when F is released
            holdTimer = 0f;
        }
    }

    void Respawn()
    {
        // Reset position and rotation
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        // Reset velocity/momentum by getting the movement script
        MoveAndRotateWheel moveScript = GetComponent<MoveAndRotateWheel>();
        if (moveScript != null)
        {
            // Reset speeds via reflection since they're private
            moveScript.GetType().GetField("bike1Speed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(moveScript, 0f);
            moveScript.GetType().GetField("bike2Speed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(moveScript, 0f);
        }
    }
}
