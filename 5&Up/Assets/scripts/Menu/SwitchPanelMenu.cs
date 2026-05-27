using UnityEngine;

public class SwitchPanelMenu : MonoBehaviour
{
    [SerializeField] private GameObject[] panels;
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
}
