using UnityEngine;
using System.Collections.Generic;

public class KickHitbox : MonoBehaviour
{
    [Header("References")]
    public Collider hitboxCollider;

    private PlayerCombat playerCombat;
    private bool attackActive;

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

    public void BeginKick(PlayerCombat combat)
    {
        playerCombat = combat;

        hitEnemies.Clear();

        attackActive = true;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }
    }

    public void EndKick()
    {
        attackActive = false;

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

        // Only hit each enemy once per kick.
        if (hitEnemies.Contains(enemyHealth))
            return;

        hitEnemies.Add(enemyHealth);

        playerCombat.KickHitEnemy(
            enemyHealth
        );
    }
}