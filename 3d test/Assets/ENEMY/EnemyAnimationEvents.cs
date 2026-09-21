using UnityEngine;

public class EnemyAnimationEvents : MonoBehaviour
{
    [Header("References")]
    public EnemyAI enemyAI;

    void Start()
    {
        if (enemyAI == null)
        {
            enemyAI = GetComponentInParent<EnemyAI>();
        }

        if (enemyAI == null)
        {
            Debug.LogError(
                "EnemyAnimationEvents: EnemyAI could not be found!"
            );
        }
    }

    // Called by the Animation Event
    public void DealAttackDamage()
    {
        if (enemyAI != null)
        {
            enemyAI.DealAttackDamage();
        }
    }
}