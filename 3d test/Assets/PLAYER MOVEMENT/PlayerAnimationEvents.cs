using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat playerCombat;

    void Start()
    {
        if (playerCombat == null)
        {
            playerCombat =
                GetComponentInParent<PlayerCombat>();
        }

        if (playerCombat == null)
        {
            Debug.LogError(
                "PlayerAnimationEvents: PlayerCombat could not be found!"
            );
        }
    }
 
    public void EndComboStep()
{
    if (playerCombat != null)
    {
        playerCombat.EndComboStep();
    }
}

public void EndCombo()
{
    if (playerCombat != null)
    {
        playerCombat.EndCombo();
    }
}

    // =========================================
    // OLD DAMAGE EVENT
    // =========================================

    // Kept so old animation events do not
    // produce a missing receiver error.
    public void DealAttackDamage()
    {
        if (playerCombat != null)
        {
            playerCombat.DealAttackDamage();
        }
    }

    // =========================================
    // SWORD
    // =========================================

    public void EnableSwordHitbox()
    {
        if (playerCombat != null)
        {
            playerCombat.EnableSwordHitbox();
        }
    }

    public void DisableSwordHitbox()
    {
        if (playerCombat != null)
        {
            playerCombat.DisableSwordHitbox();
        }
    }

    // =========================================
    // KICK
    // =========================================

    public void EnableKickHitbox()
    {
        if (playerCombat != null)
        {
            playerCombat.EnableKickHitbox();
        }
    }

    public void DisableKickHitbox()
    {
        if (playerCombat != null)
        {
            playerCombat.DisableKickHitbox();
        }
    }
}