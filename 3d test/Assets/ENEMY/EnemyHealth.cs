using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Death")]
    public float destroyDelay = 5f;

    [Header("References")]
    public Animator animator;
    public Transform lockOnPoint;
    public NavMeshAgent agent;
    public EnemyAI enemyAI;

    private float currentHealth;
    private bool isDead;

    public bool IsDead
    {
        get { return isDead; }
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );

        Debug.Log(
            "ENEMY TOOK " +
            damage +
            " DAMAGE. HEALTH: " +
            currentHealth
        );

        // Death takes priority over hit reaction.
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        // Enemy survived, so play hit reaction.
        PlayHitReaction();
    }

    void PlayHitReaction()
    {
        if (animator == null)
            return;

        animator.SetTrigger("Hit");
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("ENEMY DIED");

        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);

            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hit");

            animator.SetBool("Dead", true);
        }

        Destroy(
            gameObject,
            destroyDelay
        );
    }
}