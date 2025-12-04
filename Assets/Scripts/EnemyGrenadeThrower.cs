using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyGrenadeThrower : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 10f;
    public LayerMask playerLayer;

    [Header("Throwing")]
    public GameObject grenadePrefab; // optional: prefab should have Rigidbody and explosion script (Explode.cs)
    public Transform handTransform; // where grenade spawns (arm/hand bone)
    public float throwForce = 8f;
    public float throwUpward = 2f;
    public float cooldown = 5f;
    public float throwDelay = 0.5f; // delay between animation start and grenade release

    [Header("Animation")]
    public Animator animator; // optional: if present, will set trigger
    public string throwTrigger = "Grenade";
    // Animator speed parameter name (float) — use in a BlendTree or transitions
    public string speedParameter = "Speed";
    // Simple walk bool parameter name (Animator) - will be set true when moving
    public string walkParameter = "isWalking";

    private float lastThrowTime = -999f;
    private Transform player;
    private NavMeshAgent agent;
    private bool isDead = false;
    // Movement / grounding
    [Header("Movement")]
    public float movementSpeed = 3.5f; // used for fallback movement
    public float rotationSpeed = 8f;
    public LayerMask groundLayer = ~0;
    public float groundCheckDistance = 2f;
    public float groundOffset = 0.0f;
    private Vector3 lastMoveDir = Vector3.zero;
    private bool animatorHasSpeedParam = false;

    void Reset()
    {
        playerLayer = LayerMask.GetMask("Player");
    }

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go) player = go.transform;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        // if Animator is on a child (common for character rigs), try to find it
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // If animator uses root motion it can conflict with scripted movement.
        if (animator != null && animator.applyRootMotion)
        {
            Debug.Log("EnemyGrenadeThrower: Animator has Apply Root Motion enabled — disabling it so scripted movement controls position.\nIf you prefer root-motion-driven movement, disable this behavior and let the Animator drive locomotion.");
            animator.applyRootMotion = false;
        }

        // Try to get a NavMeshAgent only if a valid nav path exists.
        // Some users can't bake a NavMesh; in that case we fall back to simple MoveTowards movement.
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            // we found an existing agent — verify there is a valid path to the player
            if (player != null)
            {
                NavMeshPath testPath = new NavMeshPath();
                bool hasPath = NavMesh.CalculatePath(transform.position, player.position, NavMesh.AllAreas, testPath) && testPath.status == NavMeshPathStatus.PathComplete;
                if (!hasPath)
                {
                    Debug.LogWarning("EnemyGrenadeThrower: NavMeshAgent present but no valid path found. Disabling agent and using fallback movement.");
                    agent.enabled = false;
                    agent = null;
                }
                else
                {
                    agent.stoppingDistance = 1.2f;
                    agent.acceleration = 8f;
                    agent.speed = 3.5f;
                }
            }
            else
            {
                // no player yet; keep agent but configure defaults — we'll recheck in Update
                agent.stoppingDistance = 1.2f;
                agent.acceleration = 8f;
                agent.speed = 3.5f;
            }
        }
        else
        {
            // No NavMeshAgent on the object — attempt to add one only if a NavMesh path exists to the player
            if (player != null)
            {
                NavMeshPath testPath = new NavMeshPath();
                bool hasPath = NavMesh.CalculatePath(transform.position, player.position, NavMesh.AllAreas, testPath) && testPath.status == NavMeshPathStatus.PathComplete;
                if (hasPath)
                {
                    agent = gameObject.AddComponent<NavMeshAgent>();
                    agent.stoppingDistance = 1.2f;
                    agent.acceleration = 8f;
                    agent.speed = 3.5f;
                }
                else
                {
                    agent = null; // keep fallback movement
                    Debug.Log("EnemyGrenadeThrower: No NavMesh path available — using simple fallback movement.");
                }
            }
        }

        if (handTransform == null)
        {
            // try to find a child named "Hand" or "RightHand"
            handTransform = transform.Find("Hand") ?? transform.Find("RightHand");
        }

        // Basic animator sanity check
        if (animator != null)
        {
            Debug.Log("EnemyGrenadeThrower: Animator found. Using simple walk bool '" + walkParameter + "'.");
            // detect if speedParameter exists (optional fallback)
            foreach (var p in animator.parameters)
            {
                if (p.type == UnityEngine.AnimatorControllerParameterType.Float && p.name == speedParameter)
                {
                    animatorHasSpeedParam = true;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (player == null || isDead)
            return;

        float dist = Vector3.Distance(player.position, transform.position);

        // Follow the player while within detection radius
        if (dist <= detectionRadius)
        {
            if (agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                // drive animation from agent velocity using a simple walk bool
                float agentSpeed = agent.velocity.magnitude;
                if (animator != null)
                {
                    bool walking = agentSpeed > 0.1f;
                    animator.SetBool(walkParameter, walking);
                    if (!animatorHasSpeedParam)
                    {
                        // if no speed float is present, optionally set walk bool only
                    }
                    else
                    {
                        animator.SetFloat(speedParameter, agentSpeed);
                    }
                }
            }
            else
            {
                // fallback simple movement (horizontal only)
                Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
                Vector3 moveDir = (targetPos - transform.position);
                moveDir.y = 0f;
                float moveMag = moveDir.magnitude;
                if (moveMag > 0.01f)
                {
                    Vector3 moveStep = moveDir.normalized * movementSpeed * Time.deltaTime;
                    transform.position += moveStep;
                    lastMoveDir = moveDir.normalized;
                    // rotate smoothly toward movement direction
                    Quaternion tgt = Quaternion.LookRotation(lastMoveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, tgt, Time.deltaTime * rotationSpeed);
                }

                // Snap to ground using a downward raycast so enemy doesn't float
                Ray groundRay = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(groundRay, out hit, groundCheckDistance, groundLayer))
                {
                    Vector3 p = transform.position;
                    p.y = hit.point.y + groundOffset;
                    transform.position = p;
                }

                // animator: set walk bool and optional float speed
                if (animator != null)
                {
                    bool walking = moveMag > 0.01f;
                    animator.SetBool(walkParameter, walking);
                    if (animatorHasSpeedParam)
                    {
                        float animSpeed = walking ? movementSpeed : 0f;
                        animator.SetFloat(speedParameter, animSpeed);
                    }
                }
            }

            if (Time.time - lastThrowTime >= cooldown)
            {
                StartCoroutine(DoThrowRoutine());
                lastThrowTime = Time.time;
            }
        }
        else
        {
            if (agent != null)
                agent.isStopped = true;
        }
    }

    private IEnumerator DoThrowRoutine()
    {
        // Trigger animation if available
        if (animator != null && !string.IsNullOrEmpty(throwTrigger))
        {
            animator.SetTrigger(throwTrigger);
        }

        // Wait for the animation to reach the release frame
        yield return new WaitForSeconds(throwDelay);

        SpawnAndThrow();
    }

    // Called by the coroutine or can be called via Animation Event
    public void SpawnAndThrow()
    {
        if (isDead) return;

        Vector3 spawnPos;
        Quaternion spawnRot;
        if (handTransform != null)
        {
            spawnPos = handTransform.position;
            spawnRot = handTransform.rotation;
        }
        else
        {
            spawnPos = transform.position + transform.forward * 0.6f + Vector3.up * 1.2f;
            spawnRot = transform.rotation;
        }

        GameObject grenade = null;
        if (grenadePrefab != null)
        {
            grenade = Instantiate(grenadePrefab, spawnPos, spawnRot);
        }
        else
        {
            // Fallback: create a simple sphere grenade with Rigidbody and Explode script
            grenade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            grenade.transform.position = spawnPos;
            grenade.transform.rotation = spawnRot;
            grenade.transform.localScale = Vector3.one * 0.2f;
            var rb = grenade.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            // add Explode script if available in project
            var explode = grenade.AddComponent<Explode>();
            explode.timer = 3f;
            explode.radius = 5f;
            explode.power = 300f;
            // Try to auto-assign an explosion effect/audio from Resources if present
            GameObject fxRes = Resources.Load<GameObject>("ExplosionEffect");
            if (fxRes != null)
                explode.explosionEffect = fxRes;
            AudioClip sfxRes = Resources.Load<AudioClip>("ExplosionSound");
            if (sfxRes != null)
                explode.explosionSound = sfxRes;
        }

        if (grenade != null)
        {
            Rigidbody grb = grenade.GetComponent<Rigidbody>();
            if (grb == null)
                grb = grenade.AddComponent<Rigidbody>();

            // compute throw direction aiming toward player with some arc
            Vector3 dir = (player.position + Vector3.up*0.5f) - spawnPos;
            Vector3 velocity = dir.normalized * throwForce + Vector3.up * throwUpward;
            grb.velocity = velocity;
        }
    }

    // Called to transition the enemy into ragdoll/dead state
    public void EnterRagdoll()
    {
        if (isDead) return;
        isDead = true;

        // stop navigation and disable AI
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // disable animator if present
        if (animator != null)
            animator.enabled = false;

        // Enable physics on child rigidbodies to enable ragdoll
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            rb.isKinematic = false;
        }

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var col in cols)
        {
            col.enabled = true;
        }

        // Optionally add an upward impulse so the ragdoll reacts
        if (rbs.Length > 0)
        {
            rbs[0].AddForce(transform.up * 3f, ForceMode.Impulse);
        }
        else
        {
            // If no child rigidbodies (not setup as a ragdoll), fallback by adding a Rigidbody to root
            var rootRb = gameObject.AddComponent<Rigidbody>();
            rootRb.mass = 1f;
            rootRb.AddForce(transform.up * 3f, ForceMode.Impulse);
            var rootCol = GetComponent<Collider>();
            if (rootCol == null)
                gameObject.AddComponent<BoxCollider>();
        }

        // Optionally destroy this script to avoid further AI behavior
        Destroy(this);
    }

    // Optional: draw detection radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        if (handTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(handTransform.position, 0.05f);
        }
    }
}
