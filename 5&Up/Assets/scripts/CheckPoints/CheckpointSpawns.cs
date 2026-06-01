using UnityEngine;

public class CheckpointSpawns : MonoBehaviour
{
    [SerializeField]
    private int checkpointID;

    void Start()
    {
        // Register this checkpoint with the CheckPoints manager
        CheckPoints checkpointsManager = FindAnyObjectByType<CheckPoints>();
        if (checkpointsManager != null)
        {
            checkpointsManager.RegisterCheckpoint(checkpointID, transform.position, transform.rotation);
        }
    }

    public int GetCheckpointID()
    {
        return checkpointID;
    }
}
