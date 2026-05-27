using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtons : MonoBehaviour
{
    /// <summary>
    /// Load the next scene in the build settings
    /// </summary>
    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// Quit the game
    /// </summary>
    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}
