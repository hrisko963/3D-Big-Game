using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform attackPoint;
    public Animator animator;
    public NavMeshAgent agent;

    [Header("Detection")]
    public float detectionRange = 15f;

    [Header("Attack")]
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;

    [Header("Attack Damage")]
    public float attackDamage = 20f;
    public float damageRadius = 1.5f;
    public LayerMask playerLayer;

    private float nextAttackTime;

    private PlayerHealth playerHealth;


    void Start()
    {
        // Find player automatically
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        // Get PlayerHealth
        if (player != null)
        {
            playerHealth =
                player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                playerHealth =
                    player.GetComponentInParent<PlayerHealth>();
            }
        }

        // Find Animator
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Find NavMeshAgent
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        // Error checks
        if (player == null)
            Debug.LogError("ENEMY: Player is not assigned!");

        if (playerHealth == null)
            Debug.LogError("ENEMY: PlayerHealth could not be found!");

        if (animator == null)
            Debug.LogError("ENEMY: Animator is not assigned!");

        if (agent == null)
            Debug.LogError("ENEMY: NavMeshAgent is not assigned!");

        if (attackPoint == null)
            Debug.LogError("ENEMY: Attack Point is not assigned!");

        if (agent != null)
        {
            agent.updateRotation = true;
        }
    }


    void Update()
    {
        if (player == null ||
            animator == null ||
            agent == null)
        {
            return;
        }

        // =========================================
        // PLAYER IS DEAD
        // =========================================

        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            StopMoving();

            // Remove any queued attack trigger.
            animator.ResetTrigger("Attack");

            return;
        }


        // =========================================
        // DETECTION
        // =========================================

        Vector3 enemyPosition =
            transform.position;

        Vector3 playerPosition =
            player.position;

        enemyPosition.y = 0f;
        playerPosition.y = 0f;

        float detectionDistance =
            Vector3.Distance(
                enemyPosition,
                playerPosition
            );


        // =========================================
        // PLAYER NOT DETECTED
        // =========================================

        if (detectionDistance > detectionRange)
        {
            StopMoving();
            return;
        }


        // =========================================
        // ATTACK RANGE
        // =========================================

        if (attackPoint != null)
        {
            Vector3 attackPosition =
                attackPoint.position;

            Vector3 targetPosition =
                player.position;

            attackPosition.y = 0f;
            targetPosition.y = 0f;

            float attackDistance =
                Vector3.Distance(
                    attackPosition,
                    targetPosition
                );

            if (attackDistance <= attackRange)
            {
                StopMoving();
                TryAttack();
                return;
            }
        }


        // =========================================
        // CHASE
        // =========================================

        ChasePlayer();
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
        }

        animator.SetFloat(
            "Speed",
            0f,
            0.1f,
            Time.deltaTime
        );
    }


    void TryAttack()
    {
        // Don't attack dead player
        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            return;
        }

        if (Time.time < nextAttackTime)
            return;

        animator.SetTrigger("Attack");

        nextAttackTime =
            Time.time + attackCooldown;
    }


    // =============================================
    // CALLED BY ATTACK ANIMATION EVENT
    // =============================================

    public void DealAttackDamage()
    {
        // Don't damage dead player
        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            return;
        }

        if (attackPoint == null)
            return;

        Collider[] hits =
            Physics.OverlapSphere(
                attackPoint.position,
                damageRadius,
                playerLayer
            );

        foreach (Collider hit in hits)
        {
            PlayerHealth health =
                hit.GetComponentInParent<PlayerHealth>();

            if (health != null &&
                !health.IsDead)
            {
                health.TakeDamage(
                    attackDamage
                );

                // Only one hit per attack
                break;
            }
        }
    }


    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        // Attack range
        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(
                attackPoint.position,
                attackRange
            );
        }
    }
}