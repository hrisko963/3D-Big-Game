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
    public NavMeshAgent agent;
    public EnemyAI enemyAI;
    public EnemyBlock enemyBlock;
    public EnemyStagger enemyStagger;

    [Header("Lock On")]
    public Transform lockOnPoint;

    private float currentHealth;
    private bool isDead;

    public bool IsDead
    {
        get { return isDead; }
    }

    public float CurrentHealth
    {
        get { return currentHealth; }
    }

    public float MaxHealth
    {
        get { return maxHealth; }
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // IMPORTANT:
        // Your NavMeshAgent is now on EnemyRoot,
        // so we search the children.
        if (agent == null)
            agent = GetComponentInChildren<NavMeshAgent>();

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();

        if (enemyBlock == null)
            enemyBlock = GetComponent<EnemyBlock>();

        if (enemyStagger == null)
            enemyStagger = GetComponent<EnemyStagger>();

        if (animator == null)
            Debug.LogError(
                "ENEMY HEALTH: Animator not found!"
            );
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        // =========================================
        // BLOCKING
        // =========================================

        if (enemyBlock != null &&
            enemyBlock.IsBlocking)
        {
            damage = enemyBlock.BlockedDamage;

            Debug.Log(
                "ENEMY BLOCKED THE ATTACK!"
            );
        }

        // =========================================
        // DAMAGE
        // =========================================

        currentHealth -= damage;

        currentHealth =
            Mathf.Clamp(
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

        // =========================================
        // DEATH
        // =========================================

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        // =========================================
        // TRY STAGGER
        // =========================================

        // EnemyStagger itself checks whether
        // GreatSwordSlash is currently playing.
        if (enemyStagger != null)
        {
            enemyStagger.TryStagger();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("ENEMY DIED");

        // =========================================
        // DISABLE AI
        // =========================================

        if (enemyAI != null)
            enemyAI.enabled = false;

        // =========================================
        // STOP NAVMESH
        // =========================================

        if (agent != null)
        {
            if (agent.enabled &&
                agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        // =========================================
        // DISABLE BLOCK
        // =========================================

        if (enemyBlock != null)
            enemyBlock.enabled = false;

        // =========================================
        // DEATH ANIMATION
        // =========================================

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Stagger");

            animator.SetBool(
                "Blocking",
                false
            );

            animator.SetBool(
                "Dead",
                true
            );
        }

        Destroy(
            gameObject,
            destroyDelay
        );
    }
}