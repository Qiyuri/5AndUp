using UnityEngine;

public class MenuInGame : MonoBehaviour
{
    [SerializeField] private Canvas menuCanvas;

    void Start()
    {
        // Start with menu inactive
        if (menuCanvas != null)
        {
            menuCanvas.enabled = false;
        }
    }

    void Update()
    {
        // Toggle menu on Tab press
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (menuCanvas != null)
            {
                menuCanvas.enabled = !menuCanvas.enabled;
                UpdateCursorState();
            }
        }
    }

    void UpdateCursorState()
    {
        if (menuCanvas.enabled)
        {
            // Menu is active - unlock and show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Menu is inactive - lock cursor to center and hide it
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
