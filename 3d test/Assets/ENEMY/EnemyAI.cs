using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public NavMeshAgent agent;

    [Header("Detection")]
    public float detectionRange = 15f;

    [Header("Attack")]
    public float attackRange = 2.2f;
    public float attackCooldown = 1.5f;

    [Header("Rotation")]
    public float rotationSpeed = 8f;

    private float nextAttackTime;


    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }


    void Update()
    {
        if (player == null ||
            agent == null ||
            animator == null)
        {
            return;
        }

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        float distance =
            direction.magnitude;


        // -------------------------
        // IDLE
        // -------------------------

        if (distance > detectionRange)
        {
            StopMoving();
            return;
        }


        // -------------------------
        // ATTACK
        // -------------------------

        if (distance <= attackRange)
        {
            StopMoving();

            FacePlayer(direction);

            TryAttack();

            return;
        }


        // -------------------------
        // CHASE
        // -------------------------

        ChasePlayer();

        FacePlayer(direction);
    }


    void ChasePlayer()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;

        agent.SetDestination(
            player.position
        );

        animator.SetFloat(
            "Speed",
            agent.velocity.magnitude,
            0.1f,
            Time.deltaTime
        );
    }


    void StopMoving()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        animator.SetFloat(
            "Speed",
            0f,
            0.1f,
            Time.deltaTime
        );
    }


    void FacePlayer(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }


    void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        animator.SetTrigger("Attack");

        nextAttackTime =
            Time.time + attackCooldown;
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}