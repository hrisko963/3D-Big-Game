using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public PlayerBlock playerBlock;
    public PlayerParry playerParry;

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Health Regeneration")]
    public float healthRegenDelay = 30f;
    public float healthRegenAmount = 5f;
    public float healthRegenInterval = 2f;

    [Header("References")]
    public Animator animator;
    public PlayerMovement playerMovement;
    public PlayerController playerController;
    public PlayerCombat playerCombat;
    public PlayerLockOn playerLockOn;
    public PlayerDodge playerDodge;

    [Header("Death Screen")]
    public DeathScreen deathScreen;

    private float currentHealth;
    private bool isDead;

    private float lastDamageTime;
    private float nextRegenTime;

    public bool IsDead
    {
        get { return isDead; }
    }

    public float CurrentHealth
    {
        get { return currentHealth; }
    }

    public float MaxHealth
    {
        get { return maxHealth; }
    }

    void Start()
    {
        if (playerBlock == null)
            playerBlock = GetComponent<PlayerBlock>();

        if (playerParry == null)
            playerParry = GetComponent<PlayerParry>();

        currentHealth = maxHealth;

        lastDamageTime = Time.time;
        nextRegenTime =
            Time.time + healthRegenDelay;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerMovement == null)
            playerMovement =
                GetComponent<PlayerMovement>();

        if (playerController == null)
            playerController =
                GetComponent<PlayerController>();

        if (playerCombat == null)
            playerCombat =
                GetComponent<PlayerCombat>();

        if (playerLockOn == null)
            playerLockOn =
                GetComponent<PlayerLockOn>();

        if (playerDodge == null)
            playerDodge =
                GetComponent<PlayerDodge>();

        if (deathScreen == null)
            deathScreen =
                FindFirstObjectByType<DeathScreen>();

        if (animator == null)
        {
            Debug.LogError(
                "PLAYER HEALTH: Animator is not assigned!"
            );
        }

        if (deathScreen == null)
        {
            Debug.LogError(
                "PLAYER HEALTH: DeathScreen could not be found!"
            );
        }
    }

    void Update()
    {
        RegenerateHealth();
    }

    // ==================================================
    // DAMAGE
    // ==================================================

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        // ==============================================
        // PERFECT PARRY
        // ==============================================

        // Parry is checked BEFORE blocking and BEFORE
        // health is removed.
        if (playerParry != null &&
            playerParry.TryPerfectParry())
        {
            Debug.Log(
                "PLAYER PERFECTLY PARRIED THE ATTACK!"
            );

            // ZERO DAMAGE.
            return;
        }

        // ==============================================
        // NORMAL BLOCK
        // ==============================================

        if (playerBlock != null &&
            playerBlock.IsBlocking)
        {
            damage = 2f;

            Debug.Log(
                "PLAYER BLOCKED THE ATTACK!"
            );
        }

        // ==============================================
        // APPLY DAMAGE
        // ==============================================

        currentHealth -= damage;

        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );

        // Reset regeneration timer.
        lastDamageTime = Time.time;

        nextRegenTime =
            lastDamageTime +
            healthRegenDelay;

        Debug.Log(
            "PLAYER TOOK " +
            damage +
            " DAMAGE. HEALTH: " +
            currentHealth
        );

        if (currentHealth <= 0f)
            Die();
    }

    // ==================================================
    // REGENERATION
    // ==================================================

    void RegenerateHealth()
    {
        if (isDead)
            return;

        if (currentHealth >= maxHealth)
            return;

        if (Time.time < nextRegenTime)
            return;

        currentHealth +=
            healthRegenAmount;

        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );

        Debug.Log(
            "PLAYER REGENERATED " +
            healthRegenAmount +
            " HP. HEALTH: " +
            currentHealth
        );

        nextRegenTime =
            Time.time +
            healthRegenInterval;
    }

    // ==================================================
    // DEATH
    // ==================================================

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("PLAYER DIED");

        if (playerBlock != null)
            playerBlock.enabled = false;

        if (playerParry != null)
            playerParry.enabled = false;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerController != null)
            playerController.enabled = false;

        if (playerCombat != null)
            playerCombat.enabled = false;

        if (playerLockOn != null)
        {
            playerLockOn.ForceUnlock();
            playerLockOn.enabled = false;
        }

        if (playerDodge != null)
            playerDodge.enabled = false;

        if (animator != null)
        {
            animator.SetFloat(
                "WalkSpeed",
                0f
            );

            animator.SetFloat(
                "RunSpeed",
                0f
            );

            animator.ResetTrigger("Jump");
            animator.ResetTrigger("RunStop");
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Parry");

            animator.SetBool(
                "LockedOn",
                false
            );

            animator.SetBool(
                "Crouching",
                false
            );

            animator.SetBool(
                "Sliding",
                false
            );

            animator.SetBool(
                "Dead",
                true
            );
        }

        if (deathScreen != null)
        {
            deathScreen.ShowDeathScreen();
        }
    }
}