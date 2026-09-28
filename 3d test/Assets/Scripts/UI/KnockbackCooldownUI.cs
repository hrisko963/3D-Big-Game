using UnityEngine;
using UnityEngine.UI;

public class KnockbackCooldownUI : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat playerCombat;

    [Tooltip("The radial Image placed over the ability icon.")]
    public Image cooldownFill;

    void Start()
    {
        // Try to find PlayerCombat automatically
        // if it was not assigned manually.
        if (playerCombat == null)
        {
            playerCombat =
                FindFirstObjectByType<PlayerCombat>();
        }

        if (playerCombat == null)
        {
            Debug.LogError(
                "KnockbackCooldownUI: PlayerCombat could not be found!"
            );
        }

        if (cooldownFill == null)
        {
            Debug.LogError(
                "KnockbackCooldownUI: Cooldown Fill is not assigned!"
            );
        }

        UpdateCooldownUI();
    }

    void Update()
    {
        UpdateCooldownUI();
    }

    void UpdateCooldownUI()
    {
        if (playerCombat == null ||
            cooldownFill == null)
        {
            return;
        }

        // 1 = full cooldown remaining
        // 0 = ability ready
        cooldownFill.fillAmount =
            playerCombat.KnockbackCooldownNormalized;

        // Hide the overlay completely when ready.
        cooldownFill.enabled =
            !playerCombat.IsKnockbackReady;
    }
}