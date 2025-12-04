using UnityEngine;
using System.Collections;
/* ----------------------------------------
 * class to demonstrate how to apply an explosion force
 * to surrounding objects 
 */ 
public class Explode : MonoBehaviour {
	// Float variable for the radius of the area affected by the explosion
	public float radius = 20.0F;
	// Float variable for the intensity of the explosion
	public float power = 300.0F;
	// Float variable for upwards modifier of the explosion
	public float upwards = 3.0f;
	// Public float variable for time between throw and explosion
	public float timer = 5.0f;
	// Float variable for time limit before explosion  
	private float timeLimit;
    // Damage applied by the explosion (max at center)
    public int damage = 50;
	// Optional explosion visual effect (particle prefab)
	public GameObject explosionEffect;
	// Optional explosion sound
	public AudioClip explosionSound;
	// Optional override for how long to keep the effect alive (if prefab has no particle info)
	public float effectLifetime = 4f;

	/* ----------------------------------------
	 * At start, set timer limit as current time plus 'timer' variable
	 */
	void Start(){
		//Set timer limit as current time plus 'timer' variable
		timeLimit = Time.time + timer;
	}

	/* ----------------------------------------
	 * During Update, check if it's time for the explosion
	 */
	void Update(){
		if (Time.time >= timeLimit)
			// IF current time is equal or greater than timer limit, THEN call Detonate function.
			Detonate ();
	}

	/* ----------------------------------------
	 * A function for applying explosion force to surrounding objects
	 */
	public void Detonate(){
		// Vector 3 variable for the origin of the explosion, set as the object's current position
		Vector3 explosionPos = transform.position;

		// Spawn visual effect if provided
		if (explosionEffect != null)
		{
			GameObject fx = Instantiate(explosionEffect, explosionPos, Quaternion.identity);
			// If the prefab contains a particle system, schedule destruction after its duration
			var ps = fx.GetComponentInChildren<ParticleSystem>();
			if (ps != null)
			{
				Destroy(fx, ps.main.duration + ps.main.startLifetime.constantMax);
			}
			else
			{
				Destroy(fx, effectLifetime);
			}
		}

		// Play sound if provided
		if (explosionSound != null)
		{
			AudioSource.PlayClipAtPoint(explosionSound, explosionPos);
		}

		// List of colliders detected within a sphere positioned at the origin of the explosion, and radius set by user
		Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);

		// For each Collider found in the list
		foreach (Collider col in colliders) {
			// Get Rigidbody component of game object
			Rigidbody rb = col.GetComponent<Rigidbody>();
			
			if (rb != null)
				// IF game object features a rigidbody component, add explosion force passing variables set by user as parameters for strength, origin, radius and upwards modifier 
				rb.AddExplosionForce(power, explosionPos, radius, upwards);

			// Apply damage: scale by proximity (closer = more damage)
			if (damage > 0)
			{
				float dist = Vector3.Distance(col.transform.position, explosionPos);
				if (dist <= radius)
				{
					int dmg = Mathf.Max(0, Mathf.RoundToInt(damage * (1f - (dist / radius))));
					col.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
				}
			}

		}

		// Destroy grenade object once after applying forces/damage
		Destroy(gameObject);
	}
}
