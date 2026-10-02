using System.Collections.Generic;
using UnityEngine;

public static class EnemyAttackCoordinator
{
    private static readonly List<EnemyAI> enemies =
        new List<EnemyAI>();

    private static EnemyAI activeAttacker;
    private static EnemyAI lastAttacker;

    private static float nextAttackTurnTime;

    // ==================================================
    // REGISTRATION
    // ==================================================

    public static void Register(EnemyAI enemy)
    {
        if (enemy == null)
            return;

        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public static void Unregister(EnemyAI enemy)
    {
        if (enemy == null)
            return;

        enemies.Remove(enemy);

        if (activeAttacker == enemy)
        {
            activeAttacker = null;
            nextAttackTurnTime = Time.time;
        }

        if (lastAttacker == enemy)
            lastAttacker = null;
    }

    // ==================================================
    // ATTACK PERMISSION
    // ==================================================

    public static bool CanThisEnemyAttack(
        EnemyAI enemy,
        Transform player,
        float handoffDelay)
    {
        if (enemy == null || player == null)
            return false;

        CleanList();

        // Someone already owns the current attack.
        if (activeAttacker != null)
        {
            if (!activeAttacker.isActiveAndEnabled ||
                !activeAttacker.CanParticipateInAttackSelection())
            {
                activeAttacker = null;
            }
            else
            {
                return activeAttacker == enemy;
            }
        }

        // Small gap between attack turns.
        if (Time.time < nextAttackTurnTime)
            return false;

        EnemyAI selected =
            GetBestReadyAttacker(player);

        return selected == enemy;
    }

    public static bool TryClaimAttack(
        EnemyAI enemy,
        Transform player,
        float handoffDelay)
    {
        if (!CanThisEnemyAttack(
            enemy,
            player,
            handoffDelay))
        {
            return false;
        }

        activeAttacker = enemy;

        return true;
    }

    // ==================================================
    // RELEASE
    // ==================================================

    public static void ReleaseAttack(
        EnemyAI enemy,
        float handoffDelay)
    {
        if (enemy == null)
            return;

        if (activeAttacker != enemy)
            return;

        lastAttacker = enemy;
        activeAttacker = null;

        nextAttackTurnTime =
            Time.time +
            Mathf.Max(0f, handoffDelay);
    }

    public static bool IsActiveAttacker(
        EnemyAI enemy)
    {
        return enemy != null &&
               activeAttacker == enemy;
    }

    // ==================================================
    // SELECT NEXT ATTACKER
    // ==================================================

    private static EnemyAI GetBestReadyAttacker(
        Transform player)
    {
        EnemyAI closestDifferentEnemy = null;
        float closestDifferentDistance =
            Mathf.Infinity;

        EnemyAI previousEnemy = null;
        float previousEnemyDistance =
            Mathf.Infinity;

        for (int i = enemies.Count - 1;
             i >= 0;
             i--)
        {
            EnemyAI enemy = enemies[i];

            if (enemy == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            if (!enemy.isActiveAndEnabled)
                continue;

            if (!enemy.CanParticipateInAttackSelection())
                continue;

            // Must actually be ready to attack NOW.
            if (!enemy.IsReadyForAttackTurn())
                continue;

            float sqrDistance =
                enemy.GetSqrDistanceToPlayer();

            // Remember the previous attacker separately.
            if (enemy == lastAttacker)
            {
                if (sqrDistance <
                    previousEnemyDistance)
                {
                    previousEnemyDistance =
                        sqrDistance;

                    previousEnemy = enemy;
                }

                continue;
            }

            // Prefer the closest OTHER ready enemy.
            if (sqrDistance <
                closestDifferentDistance)
            {
                closestDifferentDistance =
                    sqrDistance;

                closestDifferentEnemy = enemy;
            }
        }

        // If another enemy can attack,
        // give that enemy the turn.
        if (closestDifferentEnemy != null)
            return closestDifferentEnemy;

        // If nobody else is ready,
        // previous attacker may attack again.
        return previousEnemy;
    }

    // ==================================================
    // CLEANUP
    // ==================================================

    private static void CleanList()
    {
        for (int i = enemies.Count - 1;
             i >= 0;
             i--)
        {
            if (enemies[i] == null)
                enemies.RemoveAt(i);
        }

        if (activeAttacker != null &&
            !activeAttacker.isActiveAndEnabled)
        {
            activeAttacker = null;
        }

        if (lastAttacker != null &&
            !lastAttacker.isActiveAndEnabled)
        {
            lastAttacker = null;
        }
    }
}