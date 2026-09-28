using UnityEngine;
using System.Collections.Generic;

public class SwordHitbox : MonoBehaviour
{
    [Header("References")]
    public Collider hitboxCollider;

    private PlayerCombat playerCombat;
    private bool attackActive;
    private bool applyKnockback;

    private HashSet<EnemyHealth> hitEnemies =
        new HashSet<EnemyHealth>();

    void Awake()
    {
        if (hitboxCollider == null)
        {
            hitboxCollider =
                GetComponent<Collider>();
        }

        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
        }
    }

    public void BeginAttack(
        PlayerCombat combat,
        bool knockback)
    {
        playerCombat = combat;
        applyKnockback = knockback;

        hitEnemies.Clear();

        attackActive = true;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }
    }

    public void EndAttack()
    {
        attackActive = false;
        applyKnockback = false;

        hitEnemies.Clear();

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryHitEnemy(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryHitEnemy(other);
    }

    void TryHitEnemy(Collider other)
    {
        if (!attackActive)
            return;

        if (playerCombat == null)
            return;

        EnemyHealth enemyHealth =
            other.GetComponentInParent<EnemyHealth>();

        if (enemyHealth == null)
        {
            enemyHealth =
                other.GetComponent<EnemyHealth>();
        }

        if (enemyHealth == null ||
            enemyHealth.IsDead)
        {
            return;
        }

        // Prevent the same enemy from taking
        // damage multiple times during one swing.
        if (hitEnemies.Contains(enemyHealth))
            return;

        hitEnemies.Add(enemyHealth);

        playerCombat.SwordHitEnemy(
            enemyHealth,
            applyKnockback
        );
    }
}