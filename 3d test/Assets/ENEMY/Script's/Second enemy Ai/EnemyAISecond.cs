using UnityEngine;
using UnityEngine.AI;

public class EnemyAISecond : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform attackPoint;
    public Animator animator;
    public NavMeshAgent agent;
    public Transform visualModel;

    [Header("Detection")]
    public float detectionRange = 20f;

    [Header("Combat")]
    public float combatRange = 7f;
    public float preferredDistance = 4f;
    public float attackRange = 2.5f;

    [Header("Movement")]
    public float chaseSpeed = 4f;
    public float circleSpeed = 3.5f;
    public float retreatSpeed = 4.5f;
    public float circleDistance = 2f;

    [Header("Facing")]
    public float facingOffset = 0f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;

    [Tooltip("Extra pause after an attack before moving in again.")]
    public float recoveryTime = 1f;

    [Header("Attack Damage")]
    public float attackDamage = 10f;
    public float damageRadius = 1.5f;
    public LayerMask playerLayer;

    private PlayerHealth playerHealth;

    private float nextAttackTime;
    private float retreatUntilTime;

    private int circleDirection = 1;

    public bool IsAttacking { get; private set; }


    // ==================================================
    // START
    // ==================================================

    void Start()
    {
        // Find player automatically.
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        // Find PlayerHealth.
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

        // Find Animator.
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        // Find NavMeshAgent.
        if (agent == null)
        {
            agent =
                GetComponentInChildren<NavMeshAgent>();
        }

        // Use animator object as visual model
        // if none was assigned.
        if (visualModel == null &&
            animator != null)
        {
            visualModel = animator.transform;
        }

        // Randomly choose left or right circling.
        circleDirection =
            Random.value < 0.5f ? -1 : 1;

        // Error checks.
        if (player == null)
        {
            Debug.LogError(
                "EnemyAISecond: Player not found!"
            );
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "EnemyAISecond: PlayerHealth not found!"
            );
        }

        if (animator == null)
        {
            Debug.LogError(
                "EnemyAISecond: Animator not found!"
            );
        }

        if (agent == null)
        {
            Debug.LogError(
                "EnemyAISecond: NavMeshAgent not found!"
            );
        }

        if (attackPoint == null)
        {
            Debug.LogError(
                "EnemyAISecond: AttackPoint not assigned!"
            );
        }
    }


    // ==================================================
    // UPDATE
    // ==================================================

    void Update()
    {
        if (player == null ||
            animator == null ||
            agent == null)
        {
            return;
        }

        if (!agent.isOnNavMesh)
        {
            return;
        }

        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            StopMoving();
            return;
        }

        float distance =
            FlatDistance(
                transform.position,
                player.position
            );

        // Player too far away.
        if (distance > detectionRange)
        {
            StopMoving();
            return;
        }

        // Never move during attack animation.
        if (IsAttacking)
        {
            StopMoving();
            FacePlayer();
            return;
        }

        // After attacking, back away for a moment.
        if (Time.time < retreatUntilTime)
        {
            Retreat();
            return;
        }

        // Outside combat range = chase.
        if (distance > combatRange)
        {
            ChasePlayer();
            return;
        }

        UpdateCombat(distance);
    }


    // ==================================================
    // COMBAT
    // ==================================================

    void UpdateCombat(float distance)
    {
        FacePlayer();

        float attackDistance =
            GetAttackDistance();

        // ----------------------------------------------
        // ATTACK
        // ----------------------------------------------

        if (attackDistance <= attackRange &&
            Time.time >= nextAttackTime)
        {
            Attack();
            return;
        }

        // ----------------------------------------------
        // TOO CLOSE BUT ATTACK IS ON COOLDOWN
        // ----------------------------------------------

        if (distance < 2f)
        {
            Retreat();
            return;
        }

        // ----------------------------------------------
        // MOVE CLOSER
        // ----------------------------------------------

        if (distance >
            preferredDistance + 0.5f)
        {
            MoveTowardPlayer();
            return;
        }

        // ----------------------------------------------
        // CIRCLE PLAYER
        // ----------------------------------------------

        CirclePlayer();
    }


    // ==================================================
    // CHASE
    // ==================================================

    void ChasePlayer()
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        agent.speed = chaseSpeed;
        agent.isStopped = false;

        agent.SetDestination(
            player.position
        );

        UpdateMovementAnimation();
    }


    // ==================================================
    // MOVE TOWARD PLAYER
    // ==================================================

    void MoveTowardPlayer()
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        direction.Normalize();

        // Don't run directly to the player's center.
        // Aim for a point at preferredDistance.
        Vector3 target =
            player.position -
            direction * preferredDistance;

        MoveToPosition(
            target,
            chaseSpeed
        );
    }


    // ==================================================
    // CIRCLE PLAYER
    // ==================================================

    void CirclePlayer()
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        Vector3 fromPlayer =
            transform.position -
            player.position;

        fromPlayer.y = 0f;

        if (fromPlayer.sqrMagnitude < 0.001f)
        {
            fromPlayer = player.forward;
        }

        fromPlayer.Normalize();

        Vector3 tangent =
            Vector3.Cross(
                Vector3.up,
                fromPlayer
            );

        tangent *= circleDirection;

        // Circle while maintaining preferred distance.
        Vector3 radialPosition =
            player.position +
            fromPlayer * preferredDistance;

        Vector3 target =
            radialPosition +
            tangent * circleDistance;

        MoveToPosition(
            target,
            circleSpeed
        );
    }


    // ==================================================
    // RETREAT
    // ==================================================

    void Retreat()
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        Vector3 away =
            transform.position -
            player.position;

        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
        {
            away = -player.forward;
        }

        away.Normalize();

        Vector3 target =
            transform.position +
            away * 3f;

        MoveToPosition(
            target,
            retreatSpeed
        );
    }


    // ==================================================
    // MOVE TO POSITION
    // ==================================================

    void MoveToPosition(
        Vector3 target,
        float speed)
    {
        NavMeshHit hit;

        if (NavMesh.SamplePosition(
            target,
            out hit,
            2f,
            NavMesh.AllAreas))
        {
            agent.speed = speed;
            agent.isStopped = false;

            agent.SetDestination(
                hit.position
            );

            UpdateMovementAnimation();
        }

        FacePlayer();
    }


    // ==================================================
    // ATTACK
    // ==================================================

    void Attack()
    {
        if (IsAttacking)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        StopMoving();
        FacePlayer();

        animator.ResetTrigger("Attack");

        IsAttacking = true;

        animator.SetTrigger("Attack");

        nextAttackTime =
            Time.time + attackCooldown;
    }


    // ==================================================
    // ANIMATION EVENT - DAMAGE
    // ==================================================

    public void DealAttackDamage()
    {
        if (!IsAttacking)
        {
            return;
        }

        if (attackPoint == null)
        {
            return;
        }

        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            return;
        }

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

                break;
            }
        }
    }


    // ==================================================
    // ANIMATION EVENT - END ATTACK
    // ==================================================

    public void EndAttack()
    {
        IsAttacking = false;

        // Force Enemy 2 to back away after attacking.
        retreatUntilTime =
            Time.time + recoveryTime;

        // Sometimes reverse circling direction.
        if (Random.value < 0.5f)
        {
            circleDirection *= -1;
        }
    }


    // ==================================================
    // CANCEL ATTACK
    // ==================================================

    public void CancelAttack()
    {
        IsAttacking = false;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
        }
    }


    // ==================================================
    // FACE PLAYER
    // ==================================================

    void FacePlayer()
    {
        if (visualModel == null ||
            player == null)
        {
            return;
        }

        Vector3 direction =
            player.position -
            visualModel.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion rotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

        rotation *=
            Quaternion.Euler(
                0f,
                facingOffset,
                0f
            );

        visualModel.rotation = rotation;
    }


    // ==================================================
    // STOP
    // ==================================================

    void StopMoving()
    {
        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                0f,
                0.1f,
                Time.deltaTime
            );
        }
    }


    // ==================================================
    // MOVEMENT ANIMATION
    // ==================================================

    void UpdateMovementAnimation()
    {
        if (animator == null ||
            agent == null)
        {
            return;
        }

        animator.SetFloat(
            "Speed",
            agent.velocity.magnitude,
            0.1f,
            Time.deltaTime
        );
    }


    // ==================================================
    // ATTACK DISTANCE
    // ==================================================

    float GetAttackDistance()
    {
        if (attackPoint != null)
        {
            return FlatDistance(
                attackPoint.position,
                player.position
            );
        }

        return FlatDistance(
            transform.position,
            player.position
        );
    }


    // ==================================================
    // FLAT DISTANCE
    // ==================================================

    float FlatDistance(
        Vector3 a,
        Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }


    // ==================================================
    // GIZMOS
    // ==================================================

    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;

        Gizmos.DrawWireSphere(
            center,
            detectionRange
        );

        Gizmos.DrawWireSphere(
            center,
            combatRange
        );

        Gizmos.DrawWireSphere(
            center,
            preferredDistance
        );

        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(
                attackPoint.position,
                damageRadius
            );
        }
    }
}