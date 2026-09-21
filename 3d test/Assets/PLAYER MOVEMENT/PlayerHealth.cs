using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("References")]
    public Animator animator;
    public PlayerMovement playerMovement;
    public PlayerController playerController;

    private float currentHealth;
    private bool isDead;

    public bool IsDead
    {
        get { return isDead; }
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (animator == null)
            Debug.LogError("PLAYER HEALTH: Animator is not assigned!");
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );

        Debug.Log(
            "PLAYER TOOK " +
            damage +
            " DAMAGE. HEALTH: " +
            currentHealth
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("PLAYER DIED");

        // Stop movement scripts from changing
        // movement and Animator parameters.
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerController != null)
            playerController.enabled = false;

        if (animator != null)
        {
            // Clear movement values.
            animator.SetFloat("WalkSpeed", 0f);
            animator.SetFloat("RunSpeed", 0f);

            // Clear triggers that could interrupt death.
            animator.ResetTrigger("Jump");
            animator.ResetTrigger("RunStop");

            // Enter death.
            animator.SetBool("Dead", true);
        }
    }
}