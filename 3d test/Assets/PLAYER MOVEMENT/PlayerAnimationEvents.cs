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

    // Called by the attack animation event
    public void DealAttackDamage()
    {
        if (playerCombat != null)
        {
            playerCombat.DealAttackDamage();
        }
    }
}