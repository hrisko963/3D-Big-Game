using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyStagger : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public EnemyAI enemyAI;
    public EnemyBlock enemyBlock;
    public NavMeshAgent agent;

    [Header("Stagger")]
    public float staggerDuration = 0.8f;

    private bool isStaggered;

    public bool IsStaggered
    {
        get { return isStaggered; }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();

        if (enemyBlock == null)
            enemyBlock = GetComponent<EnemyBlock>();

        if (agent == null)
            agent = GetComponentInChildren<NavMeshAgent>();

        if (animator == null)
            Debug.LogError("ENEMY STAGGER: Animator not found!");

        if (enemyAI == null)
            Debug.LogError("ENEMY STAGGER: EnemyAI not found!");

        if (agent == null)
            Debug.LogError("ENEMY STAGGER: NavMeshAgent not found!");
    }

    public void TryStagger()
    {
        if (isStaggered)
            return;

        if (enemyAI == null)
            return;

        // Only stagger while enemy attack is active.
        if (!enemyAI.IsAttacking)
            return;

        StartCoroutine(StaggerRoutine());
    }

    IEnumerator StaggerRoutine()
    {
        isStaggered = true;

        // Cancel current attack.
        enemyAI.CancelAttack();

        if (enemyBlock != null)
            enemyBlock.ForceStopBlock();

        if (animator != null)
        {
            animator.SetBool("Blocking", false);

            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Stagger");

            animator.SetTrigger("Stagger");
        }

        // Stop movement.
        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Stop EnemyAI from immediately
        // overriding the stagger.
        enemyAI.enabled = false;

        yield return new WaitForSeconds(
            staggerDuration
        );

        enemyAI.enabled = true;

        isStaggered = false;
    }
}