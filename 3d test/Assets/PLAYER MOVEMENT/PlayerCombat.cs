using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerWeapon playerWeapon;
    public PlayerLockOn playerLockOn;
    public PlayerMovement playerMovement;

    [Header("Hitboxes")]
    public KickHitbox kickHitbox;

    [Header("Attack")]
    public float attackCooldown = 0.8f;

    [Header("Stationary W/S Attacks")]
    public float stationaryAttackTime = 0.7f;

    [Header("Sword Damage")]
    public float attackDamage = 25f;

    [Header("S + Left Click Kick")]
    public float kickDamage = 10f;
    public float knockbackCooldown = 10f;
    public float knockbackDistance = 2f;
    public float knockbackDuration = 0.35f;

    [Header("Left Click Combo")]
    [Tooltip("Maximum time allowed for the complete combo before it resets.")]
    public float comboResetTime = 4f;

    private float nextAttackTime;
    private float nextKnockbackTime;

    private Coroutine movementLockCoroutine;

    // 0 = no combo
    // 1 = Combo1
    // 2 = Combo2
    // 3 = Combo3
    private int comboStep = 0;

    private float lastComboInputTime;

    // =============================================
    // KNOCKBACK COOLDOWN UI
    // =============================================

    public bool IsKnockbackReady
    {
        get
        {
            return Time.time >= nextKnockbackTime;
        }
    }

    public float KnockbackCooldownRemaining
    {
        get
        {
            return Mathf.Max(
                0f,
                nextKnockbackTime - Time.time
            );
        }
    }

    public float KnockbackCooldownNormalized
    {
        get
        {
            if (knockbackCooldown <= 0f)
                return 0f;

            return Mathf.Clamp01(
                KnockbackCooldownRemaining /
                knockbackCooldown
            );
        }
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

        if (playerLockOn == null)
        {
            playerLockOn =
                GetComponent<PlayerLockOn>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        if (kickHitbox == null)
        {
            kickHitbox =
                GetComponentInChildren<KickHitbox>(
                    true
                );
        }

        if (animator == null)
            Debug.LogError("PLAYER COMBAT: Animator is not assigned!");

        if (playerWeapon == null)
            Debug.LogError("PLAYER COMBAT: PlayerWeapon is not assigned!");

        if (playerLockOn == null)
            Debug.LogError("PLAYER COMBAT: PlayerLockOn is not assigned!");

        if (playerMovement == null)
            Debug.LogError("PLAYER COMBAT: PlayerMovement is not assigned!");

        if (kickHitbox == null)
            Debug.LogError("PLAYER COMBAT: KickHitbox could not be found!");
    }

    void Update()
    {
        if (playerWeapon == null ||
            !playerWeapon.HasWeapon)
        {
            ResetCombo();
            return;
        }

        if (playerLockOn == null ||
            !playerLockOn.IsLockedOn)
        {
            ResetCombo();
            return;
        }

        // =========================================
// AUTOMATIC COMBO RESET
// =========================================

// Once Combo3 has finished and the Animator
// has returned to LockOnMovement, reset the
// combo immediately.
if (comboStep == 3)
{
    AnimatorStateInfo state =
        animator.GetCurrentAnimatorStateInfo(0);

    if (state.IsName("LockOnMovement"))
    {
        ResetCombo();
    }
}

// Backup reset if the combo is interrupted.
if (comboStep > 0 &&
    Time.time - lastComboInputTime >
    comboResetTime)
{
    ResetCombo();
}

        if (!Input.GetMouseButtonDown(0))
            return;

        // =========================================
        // DIRECTIONAL ATTACKS
        // =========================================

        if (Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.D))
        {
            // Directional attacks still use the
            // normal attack cooldown.
            if (Time.time < nextAttackTime)
                return;

            ResetCombo();

            DirectionalAttack();
            return;
        }

        // =========================================
        // PLAIN LEFT CLICK = COMBO
        // =========================================

        HandleComboInput();
    }

    // =============================================
    // COMBO
    // =============================================

    void HandleComboInput()
    {
        if (animator == null)
            return;

        lastComboInputTime = Time.time;

        // FIRST CLICK
        if (comboStep == 0)
        {
            comboStep = 1;

            animator.ResetTrigger("Combo");
            animator.ResetTrigger("Combo2");
            animator.ResetTrigger("Combo3");

            animator.SetTrigger("Combo");

            return;
        }

        // SECOND CLICK
        //
        // This trigger can be pressed at ANY point
        // during Combo1. Animator waits for its
        // Exit Time before entering Combo2.
        if (comboStep == 1)
        {
            comboStep = 2;

            animator.SetTrigger("Combo2");

            return;
        }

        // THIRD CLICK
        //
        // Can also be pressed early. The trigger
        // remains buffered until Combo2 reaches
        // its transition Exit Time.
        if (comboStep == 2)
        {
            comboStep = 3;

            animator.SetTrigger("Combo3");

            return;
        }

        // Ignore extra clicks while Combo3 plays.
    }

    // =============================================
    // DIRECTIONAL ATTACKS
    // =============================================

    void DirectionalAttack()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("AttackForward");
        animator.ResetTrigger("AttackBack");
        animator.ResetTrigger("AttackLeft");
        animator.ResetTrigger("AttackRight");

        if (Input.GetKey(KeyCode.W))
        {
            animator.SetTrigger(
                "AttackForward"
            );

            LockMovementForStationaryAttack();
        }
        else if (Input.GetKey(KeyCode.S))
        {
            if (!IsKnockbackReady)
                return;

            animator.SetTrigger(
                "AttackBack"
            );

            LockMovementForStationaryAttack();

            nextKnockbackTime =
                Time.time + knockbackCooldown;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            animator.SetTrigger(
                "AttackLeft"
            );

            LockMovementForStationaryAttack();
        }
        else if (Input.GetKey(KeyCode.D))
        {
            animator.SetTrigger(
                "AttackRight"
            );

            LockMovementForStationaryAttack();
        }

        nextAttackTime =
            Time.time + attackCooldown;
    }

    // =============================================
    // MOVEMENT LOCK
    // =============================================

    void LockMovementForStationaryAttack()
    {
        if (playerMovement == null)
            return;

        if (movementLockCoroutine != null)
        {
            StopCoroutine(
                movementLockCoroutine
            );
        }

        movementLockCoroutine =
            StartCoroutine(
                StationaryAttackRoutine()
            );
    }

    IEnumerator StationaryAttackRoutine()
    {
        playerMovement.combatMovementLocked =
            true;

        yield return new WaitForSeconds(
            stationaryAttackTime
        );

        playerMovement.combatMovementLocked =
            false;

        movementLockCoroutine = null;
    }

    // =============================================
    // SWORD HITBOX
    // =============================================

    public void EnableSwordHitbox()
    {
        if (playerWeapon == null ||
            !playerWeapon.HasWeapon)
        {
            return;
        }

        SwordHitbox swordHitbox =
            playerWeapon.GetSwordHitbox();

        if (swordHitbox == null)
            return;

        swordHitbox.BeginAttack(
            this,
            false
        );
    }

    public void DisableSwordHitbox()
    {
        if (playerWeapon == null)
            return;

        SwordHitbox swordHitbox =
            playerWeapon.GetSwordHitbox();

        if (swordHitbox != null)
        {
            swordHitbox.EndAttack();
        }
    }

    // =============================================
    // KICK HITBOX
    // =============================================

    public void EnableKickHitbox()
    {
        if (kickHitbox == null)
            return;

        kickHitbox.BeginKick(this);
    }

    public void DisableKickHitbox()
    {
        if (kickHitbox == null)
            return;

        kickHitbox.EndKick();
    }

    // =============================================
    // SWORD DAMAGE
    // =============================================

    public void SwordHitEnemy(
        EnemyHealth enemyHealth,
        bool applyKnockback)
    {
        if (enemyHealth == null ||
            enemyHealth.IsDead)
        {
            return;
        }

        enemyHealth.TakeDamage(
            attackDamage
        );
    }

    // =============================================
    // KICK DAMAGE + KNOCKBACK
    // =============================================

    public void KickHitEnemy(
        EnemyHealth enemyHealth)
    {
        if (enemyHealth == null ||
            enemyHealth.IsDead)
        {
            return;
        }

        enemyHealth.TakeDamage(
            kickDamage
        );

        EnemyAI enemyAI =
            enemyHealth.GetComponent<EnemyAI>();

        if (enemyAI == null)
        {
            enemyAI =
                enemyHealth.GetComponentInParent<EnemyAI>();
        }

        if (enemyAI == null)
        {
            enemyAI =
                enemyHealth.GetComponentInChildren<EnemyAI>();
        }

        if (enemyAI != null)
        {
            enemyAI.ApplyKnockback(
                transform.position,
                knockbackDistance,
                knockbackDuration
            );
        }
    }

    public void EndComboStep()
{
    // If no next combo input was entered,
    // finish the combo.
    if (comboStep == 1 ||
        comboStep == 2)
    {
        ResetCombo();

        if (animator != null)
        {
            animator.CrossFade(
                "LockOnMovement",
                0.1f
            );
        }
    }
}
    
    // =============================================
    // COMBO FINISHED
    // =============================================

    // We will call this with an Animation Event
    // at the end of Combo3.
    public void EndCombo()
{
    ResetCombo();

    // Make absolutely sure the sword hitbox
    // is closed when the combo finishes.
    DisableSwordHitbox();
}

    void ResetCombo()
    {
        comboStep = 0;

        if (animator != null)
        {
            animator.ResetTrigger("Combo");
            animator.ResetTrigger("Combo2");
            animator.ResetTrigger("Combo3");
        }
    }

    // Kept temporarily for any old animation
    // events that still call DealAttackDamage.
    public void DealAttackDamage()
    {
    }

    // =============================================
    // CLEANUP
    // =============================================

    void OnDisable()
    {
        ResetCombo();

        if (playerMovement != null)
        {
            playerMovement.combatMovementLocked =
                false;
        }

        if (kickHitbox != null)
        {
            kickHitbox.EndKick();
        }

        if (playerWeapon != null)
        {
            SwordHitbox swordHitbox =
                playerWeapon.GetSwordHitbox();

            if (swordHitbox != null)
            {
                swordHitbox.EndAttack();
            }
        }
    }
}