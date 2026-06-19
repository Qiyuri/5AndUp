using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to any UI Button that should become interactable once a
/// specific checkpoint is reached. Works across scene loads automatically:
/// on Start it registers with the persistent CheckPoints singleton and
/// instantly restores its interactable state if the checkpoint was already
/// completed in a previous scene.
/// </summary>
[RequireComponent(typeof(Button))]
public class CheckpointButtonRegistrar : MonoBehaviour
{
    [Tooltip("The checkpoint ID that unlocks this button.")]
    [SerializeField] private int checkpointID;

    private Button _button;

    void Start()
    {
        _button = GetComponent<Button>();

        if (CheckPoints.Instance == null)
        {
            Debug.LogWarning($"[CheckpointButtonRegistrar] CheckPoints singleton not found — button '{name}' will not be managed.");
            return;
        }

        CheckPoints.Instance.RegisterButton(checkpointID, _button);
    }

    void OnDestroy()
    {
        // Clean up so CheckPoints never holds a reference to a destroyed Button.
        if (CheckPoints.Instance != null)
            CheckPoints.Instance.UnregisterButton(checkpointID, _button);
    }
}