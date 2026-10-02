using UnityEngine;

public class PlayerParry : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Parry Input")]
    public KeyCode parryKey = KeyCode.Q;

    [Header("Parry Timing")]
    [Tooltip("How long the perfect parry window stays active.")]
    public float perfectParryWindow = 0.15f;

    [Tooltip("Prevents parry button spamming.")]
    public float parryCooldown = 0.45f;

    private bool parryWindowActive;
    private float parryWindowEndTime;
    private float nextParryTime;

    public bool IsParryWindowActive
    {
        get { return parryWindowActive; }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError(
                "PLAYER PARRY: Animator not assigned!"
            );
        }
    }

    void Update()
    {
        if (parryWindowActive &&
            Time.time >= parryWindowEndTime)
        {
            parryWindowActive = false;
        }

        if (Input.GetKeyDown(parryKey))
        {
            StartParry();
        }
    }

    void StartParry()
    {
        if (Time.time < nextParryTime)
            return;

        nextParryTime =
            Time.time + parryCooldown;

        parryWindowActive = true;

        parryWindowEndTime =
            Time.time + perfectParryWindow;

        if (animator != null)
        {
            animator.ResetTrigger("Parry");
            animator.SetTrigger("Parry");
        }
    }

    // Called by PlayerHealth when an attack
    // reaches the player during the perfect window.
    public bool TryPerfectParry()
    {
        if (!parryWindowActive)
            return false;

        // One attack consumes the parry window.
        parryWindowActive = false;

        if (animator != null)
        {
            animator.ResetTrigger("ParrySuccess");
            animator.SetTrigger("ParrySuccess");
        }

        Debug.Log("PERFECT PARRY!");

        return true;
    }
}