using UnityEngine;

public class PlayerLockOn : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;

    [Header("Lock On")]
    public float lockOnRange = 20f;
    public LayerMask enemyLayer;

    [Header("Rotation")]
    public float rotationSpeed = 12f;

    private Transform currentTarget;
    private Transform currentLockOnPoint;
    private bool isLockedOn;

    public bool IsLockedOn
    {
        get { return isLockedOn; }
    }

    public Transform CurrentTarget
    {
        get { return currentTarget; }
    }

    public Transform LockOnPoint
    {
        get { return currentLockOnPoint; }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Toggle lock-on with Tab.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isLockedOn)
                Unlock();
            else
                FindTarget();
        }

        // Always update the Animator.
        UpdateLockOnAnimation();

        // Stop here if we are not locked onto anything.
        if (!isLockedOn || currentTarget == null)
            return;

        EnemyHealth enemyHealth =
            currentTarget.GetComponent<EnemyHealth>();

        if (enemyHealth == null)
        {
            enemyHealth =
                currentTarget.GetComponentInParent<EnemyHealth>();
        }

        // Unlock if enemy dies.
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            Unlock();
            return;
        }

        Transform distanceTarget =
            currentLockOnPoint != null
            ? currentLockOnPoint
            : currentTarget;

        float distance =
            Vector3.Distance(
                transform.position,
                distanceTarget.position
            );

        // Unlock if enemy gets too far away.
        if (distance > lockOnRange)
        {
            Unlock();
            return;
        }

        // Keep facing the enemy.
        FaceTarget();
    }

    void FindTarget()
    {
        Collider[] enemies =
            Physics.OverlapSphere(
                transform.position,
                lockOnRange,
                enemyLayer
            );

        EnemyHealth closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider enemyCollider in enemies)
        {
            EnemyHealth health =
                enemyCollider.GetComponentInParent<EnemyHealth>();

            if (health == null || health.IsDead)
                continue;

            Transform point =
                health.lockOnPoint != null
                ? health.lockOnPoint
                : health.transform;

            float distance =
                Vector3.Distance(
                    transform.position,
                    point.position
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = health;
            }
        }

        if (closestEnemy != null)
        {
            currentTarget = closestEnemy.transform;

            currentLockOnPoint =
                closestEnemy.lockOnPoint != null
                ? closestEnemy.lockOnPoint
                : closestEnemy.transform;

            isLockedOn = true;
        }
    }

    void FaceTarget()
    {
        Transform targetPoint =
            currentLockOnPoint != null
            ? currentLockOnPoint
            : currentTarget;

        Vector3 direction =
            targetPoint.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    void UpdateLockOnAnimation()
    {
        if (animator == null)
            return;

        // Tell Animator whether we are locked on.
        animator.SetBool(
            "LockedOn",
            isLockedOn
        );

        // Not locked on = no strafing.
        if (!isLockedOn)
        {
            animator.SetFloat(
                "StrafeX",
                0f,
                0.1f,
                Time.deltaTime
            );

            return;
        }

        // A = -1
        // D = +1
        float horizontal =
            Input.GetAxisRaw("Horizontal");

        animator.SetFloat(
            "StrafeX",
            horizontal,
            0.1f,
            Time.deltaTime
        );
    }

    void Unlock()
    {
        currentTarget = null;
        currentLockOnPoint = null;
        isLockedOn = false;

        // Immediately tell Animator lock-on ended.
        if (animator != null)
        {
            animator.SetBool("LockedOn", false);
            animator.SetFloat("StrafeX", 0f);
        }
    }
}