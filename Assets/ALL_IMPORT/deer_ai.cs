using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class deer_ai : MonoBehaviour
{
    [Header("Deer Settings")]
    public float health = 100f;
    public float walkRadius = 15f;
    public float detectRange = 10f;
    public float runSpeed = 5f;
    public float walkSpeed = 2f;
    public float obstacleAvoidDistance = 3f;

    [Header("References")]
    public Animator anim;
    public NavMeshAgent agent;
    public Transform player;
    public ParticleSystem bloodHitEffect;
    public ParticleSystem deathBloodEffect;

    [Header("Fade Effect")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1.5f; // Adjustable in Inspector

    private bool isDead = false;
    private bool isRunning = false;
    private bool isFading = false;

    void Start()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponent<Animator>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player").transform;

        SetRandomDestination();
        agent.speed = walkSpeed;
    }

    void Update()
    {
        if (isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < detectRange)
        {
            RunAwayFromPlayer();
        }
        else if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            SetRandomDestination();
        }

        // Handle obstacle avoidance
        AvoidWalls();

        // Update animation blend
        float speedPercent = agent.velocity.magnitude / agent.speed;
        anim.SetFloat("State", speedPercent);
    }

    void SetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, walkRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

    void RunAwayFromPlayer()
    {
        if (!isRunning)
        {
            agent.speed = runSpeed;
            isRunning = true;
        }

        Vector3 dirToPlayer = transform.position - player.position;
        Vector3 newPos = transform.position + dirToPlayer.normalized * walkRadius;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(newPos, out hit, walkRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void AvoidWalls()
    {
        Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
        if (Physics.Raycast(ray, obstacleAvoidDistance))
        {
            Vector3 newDir = Quaternion.Euler(0, Random.Range(-120, 120), 0) * transform.forward;
            agent.SetDestination(transform.position + newDir * 3f);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        if (bloodHitEffect) Instantiate(bloodHitEffect, transform.position + Vector3.up * 1f, Quaternion.identity);

        if (health <= 0f)
        {
            StartCoroutine(Die());
        }
        else
        {
            RunAwayFromPlayer();
        }
    }

    IEnumerator Die()
    {
        isDead = true;
        agent.isStopped = true;
        anim.enabled = false; // Freeze animation on death
        if (deathBloodEffect) Instantiate(deathBloodEffect, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        // Rotate deer to lie flat on the ground
        Quaternion targetRot = Quaternion.Euler(90f, transform.rotation.eulerAngles.y, 0f);
        float t = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos - new Vector3(0, 0.2f, 0);

        while (t < 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            t += Time.deltaTime * 1.5f;
            yield return null;
        }

        // Trigger camera fade effect
        if (!isFading && fadeCanvas != null)
        {
            StartCoroutine(FadeEffect());
        }
    }

    IEnumerator FadeEffect()
    {
        isFading = true;

        // Fade to black
        float t = 0f;
        while (t < fadeDuration)
        {
            fadeCanvas.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        // Fade back to normal
        t = 0f;
        while (t < fadeDuration)
        {
            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        fadeCanvas.alpha = 0f;
        isFading = false;
    }
}
