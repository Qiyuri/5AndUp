using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtons : MonoBehaviour
{
    /// <summary>
    /// Set game to singleplayer mode and load the next scene
    /// </summary>
    public void OnSingleplayerButtonClicked()
    {
        GameModeManager.SetGameMode(GameModeManager.GameMode.Singleplayer);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// Set game to multiplayer mode and load the next scene
    /// </summary>
    public void OnMultiplayerButtonClicked()
    {
        GameModeManager.SetGameMode(GameModeManager.GameMode.Multiplayer);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// Load the next scene in the build settings (legacy - now delegates to multiplayer)
    /// </summary>
    public void OnPlayButtonClicked()
    {
        OnMultiplayerButtonClicked();
    }

    /// <summary>
    /// Quit the game
    /// </summary>
    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}
