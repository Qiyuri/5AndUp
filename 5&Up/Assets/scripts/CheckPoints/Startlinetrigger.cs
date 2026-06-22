// StartLineTrigger.cs
// Place this on the same GameObject as (or near) checkpoint 0.
// It needs its own Trigger Collider — separate from the one the checkpoint
// activation system uses — so it fires EVERY time the player crosses the
// start line, not just the first time.
//
// Setup:
//   1. Add this component to a GameObject at the start line.
//   2. Add a Collider to that GameObject and tick "Is Trigger".
//   3. Make sure the player has the tag "Player" (or change playerTag below).

using UnityEngine;

public class StartLineTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            RunTimer.Instance?.OnStartLineCrossed();
    }
}