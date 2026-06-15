using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to any world-space Button that should respawn the player
/// to a specific checkpoint. It finds the persistent CheckPoints manager
/// at Start and wires itself up — no Inspector OnClick needed.
/// </summary>
public class CheckpointButton : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The checkpoint ID this button will respawn the player to.")]
    private int checkpointID;

    void Start()
    {
        Button button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"[CheckpointButton] No Button component found on {gameObject.name}!");
            return;
        }

        CheckPoints checkPoints = FindAnyObjectByType<CheckPoints>();
        if (checkPoints == null)
        {
            Debug.LogError("[CheckpointButton] Could not find CheckPoints in the scene!");
            return;
        }

        // Clear any stale Inspector listeners, then wire up fresh
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => checkPoints.RespawnToCheckpoint(checkpointID));

        Debug.Log($"[CheckpointButton] Button on '{gameObject.name}' wired to checkpoint {checkpointID}.");
    }
}