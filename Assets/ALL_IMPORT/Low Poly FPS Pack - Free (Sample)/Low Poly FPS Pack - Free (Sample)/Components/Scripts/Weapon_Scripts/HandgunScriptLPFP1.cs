using UnityEngine;
using System.Collections;
using TMPro;  // ✅ For TextMeshPro UI

public class HandgunScriptLPFP1 : MonoBehaviour
{
    private Animator anim;

    [Header("Gun Cameras")]
    public Camera gunCamera;
    public Camera mainCamera;

    [Header("FOV Settings")]
    public float fovSpeed = 15.0f;
    public float defaultFov = 40.0f;
    public float aimFov = 15.0f;

    [Header("Weapon Settings")]
    public int maxAmmo = 9;
    private int currentAmmo;
    private bool isReloading;
    private bool isAiming;
    private bool isRunning;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleParticles;
    public Light muzzleflashLight;
    public float lightDuration = 0.02f;

    [Header("Audio")]
    public AudioSource shootAudioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("UI")]
    public TMP_Text currentAmmoText;
    public TMP_Text interactPrompt;
    public string interactMessage = "Press E to pick up ammo";

    [Header("Prefabs")]
    public Transform bulletPrefab;
    public Transform casingPrefab;
    public Transform bulletSpawnPoint;
    public Transform casingSpawnPoint;

    [Header("Grenade Settings")]
    public bool enableGrenadeThrow = true;
    public Transform grenadePrefab;
    public Transform grenadeSpawnPoint;
    public float grenadeSpawnDelay = 0.35f;

    [Header("Camera Shake Settings")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.1f;

    [Header("Raycast Pickup Settings")]
    public float interactDistance = 3f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        currentAmmo = maxAmmo;

        if (muzzleflashLight != null)
            muzzleflashLight.enabled = false;
    }

    void Update()
    {
        HandleAiming();
        HandleShooting();
        HandleGrenade();
        HandleAmmoPickup();
        HandleAnimations();
        UpdateUI();
    }

    void HandleAiming()
    {
        if (Input.GetButton("Fire2") && !isReloading && !isRunning)
        {
            gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView, aimFov, fovSpeed * Time.deltaTime);
            isAiming = true;
            anim.SetBool("Aim", true);
        }
        else
        {
            gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView, defaultFov, fovSpeed * Time.deltaTime);
            isAiming = false;
            anim.SetBool("Aim", false);
        }
    }

    void HandleShooting()
	{
			Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
   			RaycastHit hit;
        if (Input.GetMouseButtonDown(0) && currentAmmo > 0 && !isReloading)
        {
            currentAmmo--;
            anim.Play(isAiming ? "Aim Fire" : "Fire", 0, 0f);
            muzzleParticles.Emit(1);
            shootAudioSource.PlayOneShot(shootSound);
            StartCoroutine(MuzzleFlashLight());
            StartCoroutine(CameraShake());
			
			if (Physics.Raycast(ray, out hit, 100f))
			{
				// Check if we hit a Deer
				if (hit.collider.CompareTag("Deer"))
				{
					deer_ai deer = hit.collider.GetComponentInParent<deer_ai>();
					if (deer != null)
					{
						deer.TakeDamage(25f); // Change damage as needed
					}
				}
			}		
            // ✅ Spawn bullet and casing
            Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            Instantiate(casingPrefab, casingSpawnPoint.position, casingSpawnPoint.rotation);
        }
    }

    void HandleGrenade()
    {
        if (!enableGrenadeThrow) return;

        if (Input.GetKeyDown(KeyCode.G))
        {
            StartCoroutine(GrenadeSpawnDelay());
            anim.Play("GrenadeThrow", 0, 0.0f);
        }
    }

    void HandleAmmoPickup()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            if (hit.collider.CompareTag("Ammo"))
            {
                if (currentAmmo < maxAmmo)
                {
                    if (interactPrompt != null)
                        interactPrompt.text = interactMessage;

                    if (Input.GetKeyDown(KeyCode.E) && !isReloading)
                    {
						StartCoroutine(PerformReload(hit.collider.gameObject));
						Destroy(hit.collider.gameObject);
                    }
                }
                else
                {
                    if (interactPrompt != null)
                        interactPrompt.text = "Ammo Full";
                }
            }
            else
            {
                if (interactPrompt != null)
                    interactPrompt.text = "";
            }
        }
        else
        {
            if (interactPrompt != null)
                interactPrompt.text = "";
        }
    }

    IEnumerator PerformReload(GameObject ammoObject)
    {
        isReloading = true;
		
        // ✅ Play reload animation
        if (anim != null)
            anim.Play("Reload", 0, 0f);

        // ✅ Play reload sound
        if (reloadSound != null)
            shootAudioSource.PlayOneShot(reloadSound);

        // ✅ Wait for reload animation duration (adjust for your clip)
        yield return new WaitForSeconds(1.3f);

        currentAmmo = maxAmmo;
        UpdateUI();
        
        isReloading = false;
    }

    IEnumerator GrenadeSpawnDelay()
    {
        yield return new WaitForSeconds(grenadeSpawnDelay);
        Instantiate(grenadePrefab, grenadeSpawnPoint.position, grenadeSpawnPoint.rotation);
    }

    IEnumerator MuzzleFlashLight()
    {
        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleflashLight.enabled = false;
    }

    IEnumerator CameraShake()
    {
        if (mainCamera == null) yield break;

        Vector3 originalPos = mainCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;

            mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = originalPos;
    }

    void HandleAnimations()
    {
        // ✅ Running state
        isRunning = Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift);
        anim.SetBool("Run", isRunning);
    }

    void UpdateUI()
    {
        if (currentAmmoText)
            currentAmmoText.text = currentAmmo + " / " + maxAmmo;
    }
}
