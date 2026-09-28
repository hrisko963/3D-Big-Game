using UnityEngine;
using System.Collections.Generic;

public class EnemyCombatSlots : MonoBehaviour
{
    public static EnemyCombatSlots Instance;

    [Header("Combat Position")]
    public float slotDistance = 4f;

    [Header("Attack Coordination")]
    [Tooltip("Delay before another enemy can attack.")]
    public float attackHandoffDelay = 0.35f;

    [Tooltip("Prevents the same enemy immediately attacking twice.")]
    public float sameEnemyDelay = 1.5f;

    private List<EnemyAI> enemies = new List<EnemyAI>();

    private EnemyAI currentAttacker;
    private EnemyAI lastAttacker;

    private float nextAttackAllowedTime;
    private float lastAttackerAllowedTime;

    void Awake()
    {
        Instance = this;
    }

    // ==================================================
    // REGISTER
    // ==================================================

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (enemy == null)
            return;

        Cleanup();

        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    // ==================================================
    // UNREGISTER
    // ==================================================

    public void UnregisterEnemy(EnemyAI enemy)
    {
        if (enemy == null)
            return;

        enemies.Remove(enemy);

        if (currentAttacker == enemy)
        {
            currentAttacker = null;
        }

        if (lastAttacker == enemy)
        {
            lastAttacker = null;
        }

        Cleanup();
    }

    // ==================================================
    // REQUEST ATTACK
    // ==================================================

    public bool RequestAttack(EnemyAI enemy)
    {
        if (enemy == null)
            return false;

        Cleanup();

        if (!enemies.Contains(enemy))
        {
            RegisterEnemy(enemy);
        }

        // This enemy already owns the attack.
        if (currentAttacker == enemy)
        {
            return true;
        }

        // Another enemy is currently attacking.
        if (currentAttacker != null)
        {
            return false;
        }

        // Wait briefly between attacks.
        if (Time.time < nextAttackAllowedTime)
        {
            return false;
        }

        // If we have more than one enemy,
        // don't immediately let the same enemy attack again.
        if (enemies.Count > 1 &&
            enemy == lastAttacker &&
            Time.time < lastAttackerAllowedTime)
        {
            return false;
        }

        // Enemy gets permission.
        currentAttacker = enemy;

        return true;
    }

    // ==================================================
    // RELEASE ATTACK
    // ==================================================

    public void ReleaseAttack(EnemyAI enemy)
    {
        if (enemy == null)
            return;

        if (currentAttacker != enemy)
            return;

        currentAttacker = null;

        lastAttacker = enemy;

        nextAttackAllowedTime =
            Time.time + attackHandoffDelay;

        lastAttackerAllowedTime =
            Time.time + sameEnemyDelay;
    }

    // ==================================================
    // CHECK ATTACKER
    // ==================================================

    public bool HasAttackSlot(EnemyAI enemy)
    {
        return currentAttacker == enemy;
    }

    // ==================================================
    // CLEANUP
    // ==================================================

    void Cleanup()
    {
        enemies.RemoveAll(enemy => enemy == null);

        if (currentAttacker != null &&
            !enemies.Contains(currentAttacker))
        {
            currentAttacker = null;
        }

        if (lastAttacker != null &&
            !enemies.Contains(lastAttacker))
        {
            lastAttacker = null;
        }
    }

    // ==================================================
    // COMBAT POSITION
    // ==================================================

    public Vector3 GetCombatPosition(
        EnemyAI enemy,
        Transform player)
    {
        if (enemy == null || player == null)
        {
            return Vector3.zero;
        }

        Cleanup();

        if (!enemies.Contains(enemy))
        {
            RegisterEnemy(enemy);
        }

        int index = enemies.IndexOf(enemy);
        int count = enemies.Count;

        if (count == 0)
        {
            return player.position;
        }

        // Spread enemies evenly around player.
        float angleStep = 360f / count;
        float angle = angleStep * index;

        Vector3 direction =
            Quaternion.Euler(
                0f,
                angle,
                0f
            ) * Vector3.forward;

        return player.position +
               direction * slotDistance;
    }
}