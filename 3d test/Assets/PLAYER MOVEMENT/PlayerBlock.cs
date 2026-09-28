using UnityEngine;

public class PlayerBlock : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerHealth playerHealth;
    public PlayerLockOn playerLockOn;

    [Header("Block")]
    public KeyCode blockKey = KeyCode.Mouse1;

    private bool isBlocking;

    public bool IsBlocking
    {
        get { return isBlocking; }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerLockOn == null)
            playerLockOn = GetComponent<PlayerLockOn>();
    }

    void Update()
    {
        // Cannot block while dead
        if (playerHealth != null && playerHealth.IsDead)
        {
            StopBlocking();
            return;
        }

        // If lock-on is lost, immediately stop blocking
        if (playerLockOn == null || !playerLockOn.IsLockedOn)
        {
            StopBlocking();
            return;
        }

        // Start blocking
        if (Input.GetKeyDown(blockKey))
        {
            StartBlocking();
        }

        // Stop blocking
        if (Input.GetKeyUp(blockKey))
        {
            StopBlocking();
        }
    }

    void StartBlocking()
    {
        // Must be locked onto an enemy
        if (playerLockOn == null || !playerLockOn.IsLockedOn)
            return;

        if (isBlocking)
            return;

        isBlocking = true;

        if (animator != null)
            animator.SetBool("Blocking", true);
    }

    void StopBlocking()
    {
        if (!isBlocking)
            return;

        isBlocking = false;

        if (animator != null)
            animator.SetBool("Blocking", false);
    }
}