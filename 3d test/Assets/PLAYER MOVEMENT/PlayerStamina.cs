using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("UI")]
public PlayerStaminaBar staminaBar;
    
    [Header("Stamina")]
    public float maxStamina = 100f;

    [Header("Sprint")]
    public float sprintDrainPerSecond = 20f;

    [Header("Dodge")]
    public float dodgeCost = 25f;

    [Header("Regeneration")]
    public float regenPerSecond = 25f;
    public float regenDelay = 1f;

    [Header("Exhaustion")]
    public float exhaustionRecoveryAmount = 20f;

    private float currentStamina;
    private float lastStaminaUseTime;

    private bool isExhausted = false;

    public float CurrentStamina
    {
        get { return currentStamina; }
    }

    public float MaxStamina
    {
        get { return maxStamina; }
    }

    public bool IsExhausted
    {
        get { return isExhausted; }
    }

    public bool HasStamina
    {
        get
        {
            return currentStamina > 0f && !isExhausted;
        }
    }

    void Start()
    {
        currentStamina = maxStamina;

        if (staminaBar == null)
    staminaBar = FindFirstObjectByType<PlayerStaminaBar>();
    }

    void Update()
    {
        RegenerateStamina();
        CheckExhaustionRecovery();
    }

    void RegenerateStamina()
    {
        if (Time.time < lastStaminaUseTime + regenDelay)
            return;

        if (currentStamina >= maxStamina)
            return;

        currentStamina += regenPerSecond * Time.deltaTime;

        currentStamina =
            Mathf.Clamp(
                currentStamina,
                0f,
                maxStamina
            );
    }

    void CheckExhaustionRecovery()
    {
        if (isExhausted &&
            currentStamina >= exhaustionRecoveryAmount)
        {
            isExhausted = false;
        }
    }

public void ShowNotEnoughStamina()
{
    if (staminaBar != null)
        staminaBar.FlashNotEnoughStamina();
}
    public void DrainSprintStamina()
    {
        if (isExhausted)
            return;

        if (currentStamina <= 0f)
        {
            BecomeExhausted();
            return;
        }

        currentStamina -=
            sprintDrainPerSecond * Time.deltaTime;

        currentStamina =
            Mathf.Clamp(
                currentStamina,
                0f,
                maxStamina
            );

        lastStaminaUseTime = Time.time;

        if (currentStamina <= 0f)
        {
            BecomeExhausted();
        }
    }

    public bool CanDodge()
    {
        if (isExhausted)
            return false;

        return currentStamina >= dodgeCost;
    }

    public bool UseDodgeStamina()
    {
        if (!CanDodge())
            return false;

        currentStamina -= dodgeCost;

        currentStamina =
            Mathf.Clamp(
                currentStamina,
                0f,
                maxStamina
            );

        lastStaminaUseTime = Time.time;

        if (currentStamina <= 0f)
        {
            BecomeExhausted();
        }

        return true;
    }

    void BecomeExhausted()
    {
        currentStamina = 0f;
        isExhausted = true;
        lastStaminaUseTime = Time.time;
    }
}