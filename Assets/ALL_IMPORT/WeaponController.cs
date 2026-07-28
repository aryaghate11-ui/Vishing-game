using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Camera playerCamera;
    private AudioSource audioSource;

    [Header("Settings")]
    public int maxAmmo = 11;
    private int currentAmmo;
    public float fireRate = 0.15f;
    public float reloadTime = 1.4f;

    [Header("Camera Shake")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.1f;

    [Header("Sound Effects")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;

    private bool isAiming;
    private bool isReloading;
    private bool canShoot = true;
    private float currentSpeed;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        if (isReloading) return;

        HandleAiming();
        HandleMovement();
        HandleShooting();
        HandleReloading();
    }

    void HandleAiming()
    {
        isAiming = Input.GetMouseButton(1); // Right click
        animator.SetBool("IsAiming", isAiming);
    }

    void HandleMovement()
    {
        float targetSpeed = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            targetSpeed = Input.GetKey(KeyCode.LeftShift) ? 1f : 0.5f;
        }

        // Smooth blending for speed
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 6f);
        animator.SetFloat("Speed", currentSpeed);
    }

    void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0) && canShoot && !isReloading)
        {
            if (currentAmmo > 0)
            {
                StartCoroutine(Shoot());
            }
            else
            {
                // Play empty click sound
                if (emptySound) audioSource.PlayOneShot(emptySound);
            }
        }
    }

    IEnumerator Shoot()
    {
        canShoot = false;
        currentAmmo--;

        // Animation and sound
        animator.SetBool("IsFiring", true);
        if (shootSound) audioSource.PlayOneShot(shootSound);

        StartCoroutine(CameraShake());

        yield return new WaitForSeconds(0.05f); // quick animation trigger
        animator.SetBool("IsFiring", false);

        yield return new WaitForSeconds(fireRate);
        canShoot = true;
    }

    void HandleReloading()
    {
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        animator.SetBool("IsReloading", true);

        if (reloadSound) audioSource.PlayOneShot(reloadSound);

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        animator.SetBool("IsReloading", false);
    }

    IEnumerator CameraShake()
    {
        Vector3 originalPos = playerCamera.transform.localPosition;

        float elapsed = 0.0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;

            playerCamera.transform.localPosition = new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.transform.localPosition = originalPos;
    }

    // Optional: simple ammo display
    void OnGUI()
    {
        GUI.Label(new Rect(15, Screen.height - 40, 200, 40),
            $"Ammo: {currentAmmo}/{maxAmmo}");
    }
}
