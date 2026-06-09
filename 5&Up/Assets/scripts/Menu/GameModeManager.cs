using UnityEngine;

/// <summary>
/// Manages the current game mode (Singleplayer or Multiplayer)
/// </summary>
public class GameModeManager : MonoBehaviour
{
    public enum GameMode
    {
        Singleplayer,
        Multiplayer
    }

    private static GameMode currentGameMode = GameMode.Multiplayer;

    public static GameMode GetGameMode()
    {
        return currentGameMode;
    }

    public static void SetGameMode(GameMode mode)
    {
        currentGameMode = mode;
        Debug.Log($"Game mode set to: {mode}");
    }

    public static bool IsSingleplayer()
    {
        return currentGameMode == GameMode.Singleplayer;
    }

    public static bool IsMultiplayer()
    {
        return currentGameMode == GameMode.Multiplayer;
    }
}
