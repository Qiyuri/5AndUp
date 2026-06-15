using UnityEngine;
using UnityEngine.UI;

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

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => checkPoints.RespawnToCheckpoint(checkpointID));

        Debug.Log($"[CheckpointButton] Button on '{gameObject.name}' wired to checkpoint {checkpointID}.");
    }
}