using UnityEngine;

// Attach to End objects (with collider marked isTrigger or with collider) and set tag to nothing special.
// This will call VictoryManager when the Player touches the trigger.
public class VictoryTrigger : MonoBehaviour
{
    public string playerTag = "Player";
    public string title = "You Win!";
    public string subtitle = "Level Complete";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (VictoryManager.Instance == null)
            {
                // create a manager if none exists
                var go = new GameObject("VictoryManager");
                go.AddComponent<VictoryManager>();
            }
            VictoryManager.Instance.ShowVictory(title, subtitle);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(playerTag))
        {
            if (VictoryManager.Instance == null)
            {
                var go = new GameObject("VictoryManager");
                go.AddComponent<VictoryManager>();
            }
            VictoryManager.Instance.ShowVictory(title, subtitle);
        }
    }
}
