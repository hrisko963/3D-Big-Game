using UnityEngine;

public class EnemyAnimationEvents : MonoBehaviour
{
    [Header("References")]
    public EnemyAI enemyAI;

    void Start()
    {
        if (enemyAI == null)
        {
            enemyAI =
                GetComponentInParent<EnemyAI>();
        }

        if (enemyAI == null)
        {
            Debug.LogError(
                "EnemyAnimationEvents: EnemyAI could not be found!"
            );
        }
    }

    // =============================================
    // ATTACK DAMAGE EVENT
    // =============================================

    public void DealAttackDamage()
    {
        if (enemyAI != null)
        {
            enemyAI.DealAttackDamage();
        }
    }

    // =============================================
    // END ATTACK EVENT
    // =============================================

    public void EndAttack()
    {
        if (enemyAI != null)
        {
            enemyAI.EndAttack();
        }
    }

    // =============================================
    // END KNOCKBACK / STANDING UP EVENT
    // =============================================

    public void EndKnockbackRecovery()
    {
        Debug.Log(
            "END KNOCKBACK RECOVERY FIRED"
        );

        if (enemyAI != null)
        {
            enemyAI.EndKnockbackRecovery();
        }
    }
}