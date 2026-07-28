using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SmoothDeerNeckRotation : MonoBehaviour
{
    [Header("References")]
    public Transform neckBone; // Assign the neck or head bone here
    public Transform player;   // For alert/looking direction

    [Header("Rotation Settings")]
    public float bodyTurnSpeed = 3f;
    public float neckTurnSpeed = 6f;
    public float neckMaxAngle = 50f;
    public float movementThreshold = 0.1f;

    private NavMeshAgent agent;
    private Animator animator;
    private Quaternion initialNeckRotation;

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

        Vector3 velocity = agent.velocity;
        bool isMoving = velocity.sqrMagnitude > movementThreshold * movementThreshold;

        // 🦴 NECK TURN (if player assigned)
        if (player && neckBone)
        {
            Vector3 toPlayer = player.position - neckBone.position;
            toPlayer.y = 0;

            Quaternion lookRot = Quaternion.LookRotation(toPlayer);
            Quaternion targetRot = Quaternion.RotateTowards(
                transform.rotation,
                lookRot,
                neckMaxAngle
            );

            neckBone.rotation = Quaternion.Slerp(
                neckBone.rotation,
                targetRot,
                Time.deltaTime * neckTurnSpeed
            );
        }

        // 🦌 BODY TURN
        if (isMoving)
        {
            Vector3 moveDir = velocity.normalized;
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * bodyTurnSpeed
            );
        }
        else if (neckBone && player)
        {
            // Reset neck rotation slowly when idle and player not visible
            neckBone.localRotation = Quaternion.Slerp(
                neckBone.localRotation,
                initialNeckRotation,
                Time.deltaTime * neckTurnSpeed * 0.5f
            );
        }
    }
}
