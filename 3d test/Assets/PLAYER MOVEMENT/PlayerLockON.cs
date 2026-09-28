using System.Collections.Generic;
using UnityEngine;

public class PlayerLockOn : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;

    [Header("Animation Smoothing")]
    [Tooltip("How smoothly lock-on movement blends between directions.")]
    public float strafeSmoothTime = 0.12f;

    [Header("Weapon")]
    public PlayerWeapon playerWeapon;

    [Header("Lock On")]
    public float lockOnRange = 20f;
    public LayerMask enemyLayer;

    [Header("Tab Controls")]
    [Tooltip("How long Tab must be held to exit lock-on.")]
    public float tabHoldTime = 0.35f;

    [Header("Rotation")]
    public float rotationSpeed = 12f;

    private Transform currentTarget;
    private Transform currentLockOnPoint;
    private bool isLockedOn;

    // Tab input
    private float tabPressedTime;
    private bool tabIsBeingHeld;
    private bool holdUnlocked;

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
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (playerWeapon == null)
        {
            playerWeapon =
                GetComponent<PlayerWeapon>();
        }
    }

    void Update()
    {
        HandleTabInput();

        UpdateLockOnAnimation();

        if (!isLockedOn ||
            currentTarget == null)
        {
            return;
        }

        EnemyHealth enemyHealth =
            currentTarget.GetComponent<EnemyHealth>();

        if (enemyHealth == null)
        {
            enemyHealth =
                currentTarget.GetComponentInParent<EnemyHealth>();
        }

        // =====================================
        // TARGET DIED
        // =====================================

        if (enemyHealth != null &&
            enemyHealth.IsDead)
        {
            SwitchToNextTarget();

            return;
        }

        // =====================================
        // TARGET OUT OF RANGE
        // =====================================

        Transform distanceTarget =
            currentLockOnPoint != null
                ? currentLockOnPoint
                : currentTarget;

        float distance =
            Vector3.Distance(
                transform.position,
                distanceTarget.position
            );

        if (distance > lockOnRange)
        {
            SwitchToNextTarget();

            return;
        }

        FaceTarget();
    }

    // =========================================
    // TAB INPUT
    // =========================================

    void HandleTabInput()
    {
        // Tab was first pressed.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            tabPressedTime = Time.time;
            tabIsBeingHeld = true;
            holdUnlocked = false;
        }

        // Tab is being held.
        if (tabIsBeingHeld &&
            Input.GetKey(KeyCode.Tab))
        {
            float heldTime =
                Time.time - tabPressedTime;

            if (!holdUnlocked &&
                heldTime >= tabHoldTime)
            {
                holdUnlocked = true;

                if (isLockedOn)
                {
                    Unlock();
                }
            }
        }

        // Tab was released.
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            tabIsBeingHeld = false;

            // If the hold action already happened,
            // don't also perform a tap.
            if (holdUnlocked)
            {
                return;
            }

            // Quick tap while already locked:
            // cycle to another enemy.
            if (isLockedOn)
            {
                SwitchToNextTarget();
            }
            else
            {
                // Quick tap while unlocked:
                // lock onto nearest enemy.
                FindTarget();
            }
        }
    }

    // =========================================
    // FIND FIRST TARGET
    // =========================================

    void FindTarget()
    {
        List<EnemyHealth> enemies =
            GetAvailableTargets();

        if (enemies.Count == 0)
        {
            Unlock();
            return;
        }

        EnemyHealth closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (EnemyHealth enemy in enemies)
        {
            Transform point =
                GetTargetPoint(enemy);

            float distance =
                Vector3.Distance(
                    transform.position,
                    point.position
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            LockOntoEnemy(closestEnemy);
        }
    }

    // =========================================
    // SWITCH TARGET
    // =========================================

    void SwitchToNextTarget()
{
    List<EnemyHealth> enemies =
        GetAvailableTargets();

    if (enemies.Count == 0)
    {
        Unlock();
        return;
    }

    // =====================================
    // SORT NEAREST → FARTHEST
    // =====================================

    enemies.Sort(
        (a, b) =>
        {
            float distanceA =
                Vector3.Distance(
                    transform.position,
                    GetTargetPoint(a).position
                );

            float distanceB =
                Vector3.Distance(
                    transform.position,
                    GetTargetPoint(b).position
                );

            return distanceA.CompareTo(distanceB);
        }
    );

    // =====================================
    // FIND CURRENT TARGET
    // =====================================

    EnemyHealth currentHealth = null;

    if (currentTarget != null)
    {
        currentHealth =
            currentTarget.GetComponent<EnemyHealth>();

        if (currentHealth == null)
        {
            currentHealth =
                currentTarget.GetComponentInParent<EnemyHealth>();
        }
    }

    // =====================================
    // FIND CURRENT TARGET IN SORTED LIST
    // =====================================

    int currentIndex =
        enemies.IndexOf(currentHealth);

    // Current target wasn't found.
    // Start with the nearest enemy.
    if (currentIndex == -1)
    {
        LockOntoEnemy(enemies[0]);
        return;
    }

    // =====================================
    // MOVE TO NEXT TARGET
    // =====================================

    int nextIndex =
        currentIndex + 1;

    // If we reached the end,
    // go back to the nearest enemy.
    if (nextIndex >= enemies.Count)
    {
        nextIndex = 0;
    }

    LockOntoEnemy(
        enemies[nextIndex]
    );
}

    // =========================================
    // GET ALL VALID TARGETS
    // =========================================

    List<EnemyHealth> GetAvailableTargets()
    {
        Collider[] colliders =
            Physics.OverlapSphere(
                transform.position,
                lockOnRange,
                enemyLayer
            );

        List<EnemyHealth> enemies =
            new List<EnemyHealth>();

        foreach (Collider enemyCollider in colliders)
        {
            EnemyHealth health =
                enemyCollider.GetComponentInParent<EnemyHealth>();

            if (health == null)
            {
                health =
                    enemyCollider.GetComponent<EnemyHealth>();
            }

            if (health == null ||
                health.IsDead)
            {
                continue;
            }

            // An enemy may have multiple colliders.
            // Don't add the same enemy more than once.
            if (enemies.Contains(health))
            {
                continue;
            }

            Transform point =
                GetTargetPoint(health);

            float distance =
                Vector3.Distance(
                    transform.position,
                    point.position
                );

            if (distance <= lockOnRange)
            {
                enemies.Add(health);
            }
        }

        return enemies;
    }

    // =========================================
    // LOCK ONTO SPECIFIC ENEMY
    // =========================================

    void LockOntoEnemy(EnemyHealth enemy)
    {
        if (enemy == null ||
            enemy.IsDead)
        {
            return;
        }

        currentTarget =
            enemy.transform;

        currentLockOnPoint =
            GetTargetPoint(enemy);

        isLockedOn = true;

        // Automatically draw the sword
        // if the player owns one.
        if (playerWeapon != null)
        {
            playerWeapon.DrawWeaponIfOwned();
        }
    }

    // =========================================
    // GET LOCK-ON POINT
    // =========================================

    Transform GetTargetPoint(
        EnemyHealth enemy)
    {
        if (enemy.lockOnPoint != null)
        {
            return enemy.lockOnPoint;
        }

        return enemy.transform;
    }

    // =========================================
    // FACE TARGET
    // =========================================

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
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    // =========================================
    // LOCK-ON ANIMATION
    // =========================================

    void UpdateLockOnAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(
            "LockedOn",
            isLockedOn
        );

        // =====================================
        // NOT LOCKED ON
        // =====================================

        if (!isLockedOn)
        {
            animator.SetFloat(
                "StrafeX",
                0f,
                strafeSmoothTime,
                Time.deltaTime
            );

            animator.SetFloat(
                "StrafeY",
                0f,
                strafeSmoothTime,
                Time.deltaTime
            );

            return;
        }

        // =====================================
        // READ MOVEMENT INPUT
        // =====================================

        float horizontal =
            Mathf.Clamp(
                Input.GetAxisRaw("Horizontal"),
                -1f,
                1f
            );

        float vertical =
            Mathf.Clamp(
                Input.GetAxisRaw("Vertical"),
                -1f,
                1f
            );

        // =====================================
        // SMOOTH ANIMATION
        // =====================================

        animator.SetFloat(
            "StrafeX",
            horizontal,
            strafeSmoothTime,
            Time.deltaTime
        );

        animator.SetFloat(
            "StrafeY",
            vertical,
            strafeSmoothTime,
            Time.deltaTime
        );
    }

    // =========================================
    // UNLOCK
    // =========================================

    void Unlock()
    {
        currentTarget = null;
        currentLockOnPoint = null;
        isLockedOn = false;

        if (animator != null)
        {
            animator.SetBool(
                "LockedOn",
                false
            );
        }
    }

    public void ForceUnlock()
    {
        Unlock();
    }
}