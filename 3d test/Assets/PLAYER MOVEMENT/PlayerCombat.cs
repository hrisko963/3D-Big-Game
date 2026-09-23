using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform attackPoint;

    [Header("Attack")]
    public float attackCooldown = 0.8f;

    [Header("Damage")]
    public float attackDamage = 25f;
    public float damageRadius = 1.5f;
    public LayerMask enemyLayer;

    private float nextAttackTime;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("PLAYER COMBAT: Animator is not assigned!");

        if (attackPoint == null)
            Debug.LogError("PLAYER COMBAT: AttackPoint is not assigned!");
    }

    void Update()
    {
        if (Time.time < nextAttackTime)
            return;

        // Left mouse button
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    void Attack()
    {
        animator.SetTrigger("Attack");

        nextAttackTime =
            Time.time + attackCooldown;
    }

    // Called by the attack animation event
    public void DealAttackDamage()
    {
        if (attackPoint == null)
            return;

        Collider[] hits =
            Physics.OverlapSphere(
                attackPoint.position,
                damageRadius,
                enemyLayer
            );

        foreach (Collider hit in hits)
        {
            EnemyHealth enemyHealth =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(
                    attackDamage
                );

                break;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(
                attackPoint.position,
                damageRadius
            );
        }
    }
}