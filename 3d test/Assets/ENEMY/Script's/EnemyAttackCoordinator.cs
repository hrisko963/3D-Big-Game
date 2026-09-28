using UnityEngine;

public static class EnemyAttackCoordinator
{
    private static EnemyAI activeAttacker;
    private static float nextAttackTurnTime;

    public static bool CanThisEnemyAttack(
        EnemyAI enemy,
        Transform player,
        float handoffDelay)
    {
        if (enemy == null || player == null)
            return false;

        if (activeAttacker != null)
            return activeAttacker == enemy;

        if (Time.time < nextAttackTurnTime)
            return false;

        return IsClosestEnemy(enemy, player);
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

    public static bool IsActiveAttacker(
        EnemyAI enemy)
    {
        return activeAttacker == enemy;
    }

    public static void ReleaseAttack(
        EnemyAI enemy,
        float handoffDelay)
    {
        if (activeAttacker != enemy)
            return;

        activeAttacker = null;

        nextAttackTurnTime =
            Time.time +
            Mathf.Max(0f, handoffDelay);
    }

    private static bool IsClosestEnemy(
        EnemyAI candidate,
        Transform player)
    {
        EnemyAI[] enemies =
            Object.FindObjectsByType<EnemyAI>(
                FindObjectsSortMode.None
            );

        EnemyAI closest = null;
        float closestDistance = Mathf.Infinity;
        int closestId = int.MaxValue;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null)
                continue;

            if (!enemy.CanParticipateInAttackSelection())
                continue;

            Vector3 difference =
                enemy.transform.position -
                player.position;

            difference.y = 0f;

            float sqrDistance =
                difference.sqrMagnitude;

            int id =
                enemy.GetInstanceID();

            if (sqrDistance < closestDistance ||
                (Mathf.Approximately(
                    sqrDistance,
                    closestDistance) &&
                 id < closestId))
            {
                closestDistance = sqrDistance;
                closestId = id;
                closest = enemy;
            }
        }

        return closest == candidate;
    }
}
