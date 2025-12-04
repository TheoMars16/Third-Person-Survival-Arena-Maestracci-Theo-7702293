using UnityEngine;

// Attach to grenade objects to detect collision with enemies and detonate immediately
public class GrenadeContact : MonoBehaviour
{
    // Optional: minimum impact speed to trigger contact
    public float impactSpeedThreshold = 1f;

    private Explode explodeComp;

    void Awake()
    {
        explodeComp = GetComponent<Explode>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // if we hit an enemy object (possibly a child collider), trigger its ragdoll behaviour and detonate immediately
        var enemy = collision.collider.GetComponentInParent<EnemyGrenadeThrower>();
        Transform root = collision.collider.transform.root;
        bool hitTaggedEnemy = collision.collider.CompareTag("Enemy") || (root != null && root.CompareTag("Enemy"));
        if (enemy != null || hitTaggedEnemy)
        {
            if (enemy != null)
            {
                Debug.Log($"GrenadeContact: hit EnemyGrenadeThrower on {enemy.gameObject.name}");
                enemy.EnterRagdoll();
            }
            else
            {
                Debug.Log($"GrenadeContact: hit GameObject tagged 'Enemy' (root: {root.name}), sending EnterRagdoll");
                root.SendMessage("EnterRagdoll", SendMessageOptions.DontRequireReceiver);
            }

            if (explodeComp != null)
            {
                explodeComp.Detonate();
            }
            else
            {
                Destroy(gameObject);
            }

            return;
        }

        // Optionally detonate on strong impacts with any object
        if (collision.relativeVelocity.magnitude >= impactSpeedThreshold)
        {
            if (explodeComp != null)
                explodeComp.Detonate();
            else
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // also support trigger-based mines/enemies (check parent hierarchy and root tag)
        var enemy = other.GetComponentInParent<EnemyGrenadeThrower>();
        Transform root = other.transform.root;
        bool hitTaggedEnemy = other.CompareTag("Enemy") || (root != null && root.CompareTag("Enemy"));
        if (enemy != null || hitTaggedEnemy)
        {
            if (enemy != null)
            {
                Debug.Log($"GrenadeContact: trigger hit EnemyGrenadeThrower on {enemy.gameObject.name}");
                enemy.EnterRagdoll();
            }
            else
            {
                Debug.Log($"GrenadeContact: trigger hit GameObject tagged 'Enemy' (root: {root.name}), sending EnterRagdoll");
                root.SendMessage("EnterRagdoll", SendMessageOptions.DontRequireReceiver);
            }

            if (explodeComp != null)
                explodeComp.Detonate();
            else
                Destroy(gameObject);
        }
    }
}
