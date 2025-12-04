using UnityEngine;
using System.Collections;

public class Mine : MonoBehaviour 
{
    // Customizeable Variables
    public GameObject explosion;
    [Range(0.5f, 3f)]
    public float explodeTime = 1f;
    [Range(5, 50)]
    public int damage = 10;

    private void OnTriggerEnter(Collider other)
    {
        // Trigger explosion if player or an enemy enters the mine trigger
        if (other.CompareTag("Player") || other.CompareTag("Enemy") || other.GetComponent<EnemyGrenadeThrower>() != null)
        {
            var aud = GetComponent<AudioSource>();
            if (aud != null) aud.Play();
            Invoke("Explode", explodeTime);
        }
    }

    private void Explode()
    {
        Instantiate(explosion, transform.position, Quaternion.identity);
        Collider[] cols = Physics.OverlapSphere(transform.position, 2);
        foreach (Collider col in cols)
        {
            // Damage players
            if (col.CompareTag("Player"))
                col.SendMessage("TakeDamage", damage);

            // If an enemy was caught in the explosion, trigger its ragdoll/death
            col.SendMessage("EnterRagdoll", SendMessageOptions.DontRequireReceiver);
        }
        Destroy(gameObject);
    }
}
