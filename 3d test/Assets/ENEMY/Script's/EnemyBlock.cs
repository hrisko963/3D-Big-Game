using UnityEngine;
using System.Collections;

public class EnemyBlock : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public EnemyHealth enemyHealth;

    [Header("Block Settings")]
    [Range(0f, 1f)]
    public float blockChance = 0.5f;

    public float blockDuration = 1.5f;
    public float blockCooldown = 2f;

    private bool isBlocking;
    private bool blockOnCooldown;

    public bool IsBlocking
    {
        get { return isBlocking; }
    }

    public float BlockedDamage
    {
        get { return 2f; }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
    }

    public void TryBlock()
    {
        if (isBlocking)
            return;

        if (blockOnCooldown)
            return;

        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (Random.value <= blockChance)
        {
            StartCoroutine(BlockRoutine());
        }
        else
        {
            StartCoroutine(BlockCooldownRoutine());
        }
    }

    IEnumerator BlockRoutine()
    {
        isBlocking = true;
        blockOnCooldown = true;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetBool("Blocking", true);
        }

        yield return new WaitForSeconds(blockDuration);

        StopBlocking();

        yield return new WaitForSeconds(blockCooldown);

        blockOnCooldown = false;
    }

    IEnumerator BlockCooldownRoutine()
    {
        blockOnCooldown = true;

        yield return new WaitForSeconds(blockCooldown);

        blockOnCooldown = false;
    }

    public void StopBlocking()
    {
        if (!isBlocking)
            return;

        isBlocking = false;

        if (animator != null)
            animator.SetBool("Blocking", false);
    }

    public void ForceStopBlock()
    {
        StopAllCoroutines();

        isBlocking = false;
        blockOnCooldown = false;

        if (animator != null)
            animator.SetBool("Blocking", false);
    }
}