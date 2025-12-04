using UnityEngine;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    // Object Static Variables
    private static int _maxHealth = 100;
    private static int _maxAmmo = 250;
    public static int maxHealth
    {
        get { return _maxHealth; }
    }
    public static int maxAmmo
    {
        get { return _maxAmmo; }
    }
    public static GameObject instance;

    // Reference Variables
    private AudioSource source;
    private CharacterController control;
    private Transform cam;

    // Public Reference Variables
    [HideInInspector]
    public Transform weaponSlot;
    public bool hasKey
    {
        get { return gotKey; }
    }
    public List<string> hasItems
    {
        get { return gotItems; }
    }

    // Object Variables
    private Transform spawnPoint;
    private Vector3 moveDirection;
    private Vector3 camRotation;
    private int curHealth = _maxHealth;
    private int curAmmo;
    private List<string> gotItems = new List<string>(4);
    private bool gotWeapon;
    private bool gotKey;
    private float walkTime;

    // Events
    public delegate void HealthUpdateHandler(int curHealth);
    public static event HealthUpdateHandler HealthUpdate;
    public delegate void AmmoUpdateHandler(int curAmmo);
    public static event AmmoUpdateHandler AmmoUpdate;
    public delegate void ItemUpdateHandler(string itemName);
    public static event ItemUpdateHandler ItemUpdate;
    public delegate void PauseHandler(bool pause);
    public static event PauseHandler PauseEvent;

    // Customizeable Variables
    public GameObject riflePrefab;
    public GameObject feedbackPrefab;
    public GameObject grenadePrefab;
    public Transform bulletOrigin;    
    public float grenadeThrowForce = 10f;
    public float grenadeThrowUpward = 2f;
    public float speed = 6;
    public float jumpSpeed = 1.5f;
    public float gravity = 0.5f;
    [Range(10, 200)]
    public int shootRange = 100;
    [Range(-45, -15)]
    public int minAngle = -30;
    [Range(30, 80)]
    public int maxAngle = 45;
    [Range(50, 500)]
    public int sensitivity = 200;
    [Range(5, 50)]
    public int healAmount = 25;
    [Range(20, 100)]
    public int ammoAmount = 40;
    public AudioClip walkSound;
    public AudioClip shootSound;
    public AudioClip dryFireSound;
    public AudioClip hitSound;
    public AudioClip pickAmmoSound;
    public AudioClip pickHealthSound;
    public AudioClip pickSound;
    
    // MonoBehaviour Functions
    private void Awake()
    {
        instance = gameObject;
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        control = GetComponent<CharacterController>();
        if (control == null)
        {
            Debug.LogWarning("Player: CharacterController missing on GameObject. Adding one at runtime.");
            control = gameObject.AddComponent<CharacterController>();
        }

        // Camera lookup with fallbacks
        Camera mainCam = Camera.main;
        if (mainCam == null)
            mainCam = GetComponentInChildren<Camera>();
        if (mainCam != null)
        {
            cam = mainCam.transform;
            // try to get weapon slot safely
            if (cam.childCount > 0)
                weaponSlot = cam.GetChild(0);
        }
        else
        {
            Debug.LogWarning("Player: No Main Camera found. Camera-based behaviour may be disabled.");
            cam = transform;
            weaponSlot = transform;
        }

        // Ensure spawnPoint fallback so TakeDamage / respawn won't NRE
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    private void Update()
    {
        // Left click throws a grenade
        if (Input.GetMouseButtonDown(0))
            ThrowGrenade();

        Shoot();
        Rotate();
        Move();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool pause = Time.timeScale > 0 ? true : false;
            PauseEvent?.Invoke(pause);
        }
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Bypass();
        #endif
    }

    private void ThrowGrenade()
    {
        Vector3 spawnPos = (weaponSlot != null) ? weaponSlot.position : transform.position + transform.forward * 0.6f + Vector3.up * 1.2f;
        Quaternion spawnRot = (cam != null) ? Quaternion.LookRotation(cam.forward) : transform.rotation;

        GameObject grenade = null;
        if (grenadePrefab != null)
        {
            grenade = Instantiate(grenadePrefab, spawnPos, spawnRot);
        }
        else
        {
            grenade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            grenade.transform.position = spawnPos;
            grenade.transform.rotation = spawnRot;
            grenade.transform.localScale = Vector3.one * 0.2f;
        }

        if (grenade != null)
        {
            // Ensure Rigidbody
            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb == null) rb = grenade.AddComponent<Rigidbody>();

            // Ensure Explode exists
            Explode ex = grenade.GetComponent<Explode>();
            if (ex == null)
            {
                ex = grenade.AddComponent<Explode>();
                ex.timer = 3f;
                ex.radius = 5f;
                ex.power = 300f;
            }

            // Ensure GrenadeContact
            GrenadeContact gc = grenade.GetComponent<GrenadeContact>();
            if (gc == null) gc = grenade.AddComponent<GrenadeContact>();

            // Apply throw velocity towards crosshair
            Camera c = Camera.main != null ? Camera.main : (cam != null ? cam.GetComponent<Camera>() : null);
            Vector3 dir;
            if (c != null)
            {
                Vector3 target = c.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 20f));
                dir = (target - spawnPos).normalized;
            }
            else
            {
                dir = transform.forward;
            }

            Vector3 velocity = dir * grenadeThrowForce + Vector3.up * grenadeThrowUpward;
            rb.velocity = velocity;

            // If the prefab had an Explode component with explosionEffect assigned, keep it; else try Resources
            if (ex.explosionEffect == null)
            {
                GameObject fxRes = Resources.Load<GameObject>("ExplosionEffect");
                if (fxRes != null) ex.explosionEffect = fxRes;
            }
            if (ex.explosionSound == null)
            {
                AudioClip sfxRes = Resources.Load<AudioClip>("ExplosionSound");
                if (sfxRes != null) ex.explosionSound = sfxRes;
            }
        }
    }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Bypass()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            gotKey = true;
        else if (Input.GetKeyDown(KeyCode.F2))
            GetItem("Battery");
        else if (Input.GetKeyDown(KeyCode.F3))
            GetItem("SmallBattery");
        else if (Input.GetKeyDown(KeyCode.F4))
            GetItem("MediumBattery");
        else if (Input.GetKeyDown(KeyCode.F5))
            GetItem("GasCan");
    }
    #endif

    /// <summary>
    /// Controls camera rotation oriented by mouse position
    /// </summary>
    private void Rotate()
    {
        transform.Rotate(Vector3.up * sensitivity * Time.deltaTime * Input.GetAxis("MouseX"));

        float mouseY = 0f;
        if (Input.GetAxis("MouseY") != 0f)
            mouseY = Input.GetAxis("MouseY");

        camRotation.x -= mouseY * sensitivity * Time.deltaTime;
        camRotation.x = Mathf.Clamp(camRotation.x, minAngle, maxAngle);

        if (cam != null)
            cam.localEulerAngles = camRotation;
    }

    /// <summary>
    /// Controls movement with CharacterController
    /// </summary>
    private void Move()
    {
        if (control.isGrounded)
        {
            float h = 0f;
            float v = 0f;
            if (Input.GetKey(KeyCode.Q)) h -= 1f;
            if (Input.GetKey(KeyCode.D)) h += 1f;
            if (Input.GetKey(KeyCode.Z)) v += 1f;
            if (Input.GetKey(KeyCode.S)) v -= 1f;

            moveDirection = new Vector3(h, 0, v);
            if (moveDirection.magnitude > 1f)
                moveDirection = moveDirection.normalized;
            moveDirection = transform.TransformDirection(moveDirection);

            if (moveDirection.magnitude > 0.3f && walkTime > 0.4f)
            {
                walkTime = 0;
                source.pitch = Random.Range(0.8f, 1.2f);
                source.PlayOneShot(walkSound, 0.3f);
            }
            walkTime += Time.deltaTime;

            if (Input.GetButtonDown("Jump"))
                moveDirection.y = jumpSpeed;            
        }

        moveDirection.y -= gravity * Time.deltaTime;
        control.Move(moveDirection * speed * Time.deltaTime);
    }

    /// <summary>
    /// Raycasts to the target position, and sends a message to the object if hit
    /// </summary>
    private void Shoot()
    {
        if (!gotWeapon || !Input.GetButtonDown("Fire"))
            return;

        if (curAmmo <= 0)
        {
            source.pitch = Random.Range(0.8f, 1.2f);
            source.PlayOneShot(dryFireSound);
            return;
        }

        // Play shoot audio and feedback
        source.pitch = Random.Range(0.8f, 1.2f);
        source.PlayOneShot(shootSound);
        curAmmo--;
        AmmoUpdate?.Invoke(curAmmo);
        camRotation.x -= 2;

        if (bulletOrigin != null && feedbackPrefab != null)
        {
            GameObject gunFlare = Instantiate(feedbackPrefab, bulletOrigin.position, Quaternion.identity) as GameObject;
            gunFlare.transform.SetParent(bulletOrigin);
            Destroy(gunFlare, 0.1f);
        }

        // Shoot ray
        Camera c = Camera.main != null ? Camera.main : (cam != null ? cam.GetComponent<Camera>() : null);
        Vector3 origin = (cam != null) ? cam.position : transform.position;
        Vector3 dir;
        if (c != null)
        {
            Vector3 target = c.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, shootRange));
            dir = target - origin;
        }
        else
        {
            dir = transform.forward;
        }

        Ray ray = new Ray(origin, dir.normalized);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, shootRange))
        {
            hit.collider.SendMessage("TakeDamage", SendMessageOptions.DontRequireReceiver);
            if (feedbackPrefab != null)
            {
                GameObject hitFlare = Instantiate(feedbackPrefab, hit.point, Quaternion.identity) as GameObject;
                Destroy(hitFlare, 0.2f);
            }
        }
    }

    /// <summary>
    /// Updates the player spawnpoint by a given point
    /// </summary>
    /// <param name="newPoint">The new spawnpoint's transform</param>
    private void UpdateSpawnpoint(Transform newPoint)
    {
        spawnPoint = newPoint;
    }

    /// <summary>
    /// Takes a given amount of damage
    /// </summary>
    /// <param name="amount">The amount of damage to take</param>
    private void TakeDamage(int amount)
    {
        curHealth -= amount;        
        if (curHealth <= 0)
        {
            curHealth = 0;
            // On death: show defeat UI with single Restart button and disable player control
            if (VictoryManager.Instance == null)
            {
                var go = new GameObject("VictoryManager");
                go.AddComponent<VictoryManager>();
            }
            VictoryManager.Instance.ShowDefeat("You Died", "Press Restart to try again");

            // Disable player control to prevent input while dead
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            this.enabled = false;
        }
        source.pitch = Random.Range(0.8f, 1.2f);
        source.PlayOneShot(hitSound);
        HealthUpdate?.Invoke(curHealth);
    }

    /// <summary>
    /// Recovers the defaul amount of damage
    /// </summary>
    private void RecoverHealth()
    {
        if (curHealth == _maxHealth)
            return;

        curHealth += healAmount;
        if (curHealth > _maxHealth)
            curHealth = _maxHealth;

        source.pitch = Random.Range(0.8f, 1.2f);
        source.PlayOneShot(pickHealthSound);
        HealthUpdate?.Invoke(curHealth);
    }

    /// <summary>
    /// Collects a given amount of ammunition
    /// </summary>
    /// <param name="amount">The amount of ammo to pick</param>
    private void TakeAmmo(int amount)
    {
        curAmmo += amount;
        if (curAmmo > _maxAmmo)
            curAmmo = _maxAmmo;

        source.pitch = Random.Range(0.8f, 1.2f);
        source.PlayOneShot(pickAmmoSound);
        AmmoUpdate?.Invoke(curAmmo);
    }

    /// <summary>
    /// Gets the rifle and equips it
    /// </summary>
    private void GetRifle()
    {
        source.pitch = Random.Range(0.8f, 1.2f);
        source.PlayOneShot(pickAmmoSound);
        GameObject rifle = Instantiate(riflePrefab, weaponSlot.position, Quaternion.identity) as GameObject;
        rifle.transform.SetParent(weaponSlot);        
        rifle.transform.localScale = Vector3.one * 0.2f;
        rifle.transform.localRotation = Quaternion.identity;
        rifle.transform.localPosition = Vector3.zero;
        gotWeapon = true;
        TakeAmmo(20);
    }

    /// <summary>
    /// Gets an item and process its required logic
    /// </summary>
    /// <param name="itemName">The item to get</param>
    private void GetItem(string itemName)
    {
        switch (itemName)
        {
            case "Key":
                gotKey = true;
                source.pitch = Random.Range(0.8f, 1.2f);
                source.PlayOneShot(pickSound);
                break;
            case "Rifle":
                GetRifle();
                break;
            case "Health":
                RecoverHealth();
                break;
            case "Ammo":
                TakeAmmo(ammoAmount);
                break;
            default:
                gotItems.Add(itemName);
                source.pitch = Random.Range(0.8f, 1.2f);
                source.PlayOneShot(pickSound);
                    ItemUpdate?.Invoke(itemName);
                break;
        }
    }
}
