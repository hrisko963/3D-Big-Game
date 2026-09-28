using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public PlayerBlock playerBlock;

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

        currentHealth = maxHealth;

        lastDamageTime = Time.time;
        nextRegenTime = Time.time + healthRegenDelay;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerCombat == null)
            playerCombat = GetComponent<PlayerCombat>();

        if (playerLockOn == null)
            playerLockOn = GetComponent<PlayerLockOn>();

        if (playerDodge == null)
            playerDodge = GetComponent<PlayerDodge>();

        if (deathScreen == null)
            deathScreen = FindFirstObjectByType<DeathScreen>();

        if (animator == null)
            Debug.LogError("PLAYER HEALTH: Animator is not assigned!");

        if (deathScreen == null)
            Debug.LogError("PLAYER HEALTH: DeathScreen could not be found!");
    }

    void Update()
    {
        RegenerateHealth();
    }

    public void TakeDamage(float damage)
{
    if (isDead)
        return;

    if (playerBlock != null && playerBlock.IsBlocking)
    {
        damage = 2f;

        Debug.Log("PLAYER BLOCKED THE ATTACK!");
    }

    currentHealth -= damage;

        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );

        // Reset health regeneration timer
        lastDamageTime = Time.time;
        nextRegenTime = lastDamageTime + healthRegenDelay;

        Debug.Log(
            "PLAYER TOOK " +
            damage +
            " DAMAGE. HEALTH: " +
            currentHealth
        );

        if (currentHealth <= 0f)
            Die();
    }

    void RegenerateHealth()
    {
        // Dead players cannot regenerate
        if (isDead)
            return;

        // Don't regenerate if already full
        if (currentHealth >= maxHealth)
            return;

        // Wait until regeneration time
        if (Time.time < nextRegenTime)
            return;

        // Heal
        currentHealth += healthRegenAmount;

        currentHealth = Mathf.Clamp(
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

        // Wait before next regeneration
        nextRegenTime =
            Time.time + healthRegenInterval;
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("PLAYER DIED");

        // Disable gameplay controls
        if (playerBlock != null)
        playerBlock.enabled = false;

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

        // Play death animation
        if (animator != null)
        {
            animator.SetFloat("WalkSpeed", 0f);
            animator.SetFloat("RunSpeed", 0f);

            animator.ResetTrigger("Jump");
            animator.ResetTrigger("RunStop");
            animator.ResetTrigger("Attack");

            animator.SetBool("LockedOn", false);
            animator.SetBool("Dead", true);
        }

        // Show death UI
        if (deathScreen != null)
            deathScreen.ShowDeathScreen();
    }
}