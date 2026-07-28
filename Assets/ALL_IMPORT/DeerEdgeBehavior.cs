using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DeerEdgeBehavior : MonoBehaviour
{
    [Header("References")]
    public Transform neckBone;      // Assign neck or head bone here
    public Transform player;        // Assign player/camera here

    [Header("Rotation Settings")]
    public float bodyTurnSpeed = 3f;
    public float neckTurnSpeed = 6f;
    public float neckMaxAngle = 50f;
    public float idleHeadScanSpeed = 25f;

    [Header("Detection Settings")]
    public float stopDistanceFromEdge = 1.2f;
    public LayerMask groundMask;

    private NavMeshAgent agent;
    private Animator animator;
    private Quaternion initialNeckRotation;
    private bool isAtEdge = false;
    private bool isIdleLooking = false;
    private float randomLookTimer = 0f;
    private float lookDirection = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.updateRotation = false;

        if (neckBone != null)
            initialNeckRotation = neckBone.localRotation;
    }

    void Update()
    {
        if (!agent.isOnNavMesh) return;

        // --- Check if near edge ---
        isAtEdge = !IsNavMeshAhead();

        if (isAtEdge)
        {
            // Stop the agent and look around
            agent.isStopped = true;
            animator.SetFloat("State", 0f);

            if (!isIdleLooking)
                StartCoroutine(IdleHeadLook());
        }
        else
        {
            agent.isStopped = false;
        }

        // --- Smooth Body Rotation if Moving ---
        if (!isAtEdge && agent.velocity.sqrMagnitude > 0.05f)
        {
            Vector3 moveDir = agent.velocity.normalized;
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * bodyTurnSpeed);
        }

        // --- Head Tracking (player focus if nearby) ---
        if (player && neckBone)
        {
            Vector3 toPlayer = player.position - neckBone.position;
            toPlayer.y = 0f;

            // Rotate head toward player if in front, else use idle scan rotation
            if (Vector3.Dot(transform.forward, toPlayer.normalized) > 0.2f)
            {
                Quaternion lookRot = Quaternion.LookRotation(toPlayer.normalized);
                Quaternion limitedRot = Quaternion.RotateTowards(transform.rotation, lookRot, neckMaxAngle);
                neckBone.rotation = Quaternion.Slerp(neckBone.rotation, limitedRot, Time.deltaTime * neckTurnSpeed);
            }
            else if (isIdleLooking)
            {
                // idle scanning handled by coroutine
            }
            else
            {
                // reset neck
                neckBone.localRotation = Quaternion.Slerp(neckBone.localRotation, initialNeckRotation, Time.deltaTime * neckTurnSpeed * 0.5f);
            }
        }
    }

    // 🧭 Check if there’s navmesh or ground in front of deer
    bool IsNavMeshAhead()
    {
        Vector3 checkPos = transform.position + transform.forward * stopDistanceFromEdge;
        NavMeshHit hit;
        return NavMesh.SamplePosition(checkPos, out hit, 1f, NavMesh.AllAreas);
    }

    // 🦌 Look around naturally when idle
    IEnumerator IdleHeadLook()
    {
        isIdleLooking = true;
        while (isAtEdge)
        {
            yield return new WaitForSeconds(Random.Range(1.5f, 3f));
            lookDirection = Random.Range(-neckMaxAngle, neckMaxAngle);
            float t = 0f;
            while (t < 1f && isAtEdge)
            {
                Quaternion targetRot = Quaternion.Euler(0, lookDirection, 0) * transform.rotation;
                neckBone.rotation = Quaternion.Slerp(neckBone.rotation, targetRot, Time.deltaTime * idleHeadScanSpeed);
                t += Time.deltaTime;
                yield return null;
            }
        }
        isIdleLooking = false;
    }
}
