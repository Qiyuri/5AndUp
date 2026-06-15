using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    void Awake()
    {
        // Only one GameManager should ever exist
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}