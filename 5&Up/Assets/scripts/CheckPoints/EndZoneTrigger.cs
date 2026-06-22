// EndZoneTrigger.cs
// Zet dit op een leeg GameObject met een Trigger-Collider als eindlijn.
// Zorg dat de speler de tag "Player" heeft (of pas playerTag aan).

using UnityEngine;

public class EndZoneTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            RunTimer.Instance?.OnRunFinished();
    }
}
