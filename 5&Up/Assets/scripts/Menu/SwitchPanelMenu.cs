using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchPanelMenu : MonoBehaviour
{
    [SerializeField] private GameObject[] panels;
    [SerializeField] private string[] sceneNames;
    private int currentPanelIndex = 0;

    void Start()
    {
        // Activate first panel, deactivate others
        if (panels.Length > 0)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].SetActive(i == 0);
            }
        }
    }

    /// <summary>
    /// Switches to the specified panel index
    /// </summary>
    public void SwitchPanel(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= panels.Length)
        {
            Debug.LogWarning($"Panel index {panelIndex} is out of range!");
            return;
        }

        // Deactivate current panel
        if (currentPanelIndex >= 0 && currentPanelIndex < panels.Length)
        {
            panels[currentPanelIndex].SetActive(false);
        }

        // Activate new panel
        panels[panelIndex].SetActive(true);
        currentPanelIndex = panelIndex;
    }

    /// <summary>
    /// Switches to the next panel in the array
    /// </summary>
    public void NextPanel()
    {
        int nextIndex = (currentPanelIndex + 1) % panels.Length;
        SwitchPanel(nextIndex);
    }

    /// <summary>
    /// Switches to the previous panel in the array
    /// </summary>
    public void PreviousPanel()
    {
        int prevIndex = (currentPanelIndex - 1 + panels.Length) % panels.Length;
        SwitchPanel(prevIndex);
    }

    /// <summary>
    /// Switches to a scene by index
    /// </summary>
    public void SwitchScene(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= sceneNames.Length)
        {
            Debug.LogWarning($"Scene index {sceneIndex} is out of range!");
            return;
        }
        SceneManager.LoadScene(sceneNames[sceneIndex]);
    }

    /// <summary>
    /// Switches to a scene by name
    /// </summary>
    public void SwitchSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name cannot be null or empty!");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
}
