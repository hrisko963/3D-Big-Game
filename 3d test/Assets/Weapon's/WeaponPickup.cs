using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Pickup")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("Weapon")]
    public GameObject weaponObject;

    private bool playerInRange;
    private PlayerWeapon playerWeapon;

    void Start()
    {
        if (weaponObject == null)
            weaponObject = gameObject;
    }

    void Update()
    {
        if (!playerInRange)
            return;

        if (playerWeapon == null)
            return;

        if (Input.GetKeyDown(pickupKey))
        {
            playerWeapon.PickUpWeapon(
                weaponObject
            );

            // Disable the pickup object after
            // giving the weapon to the player.
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerWeapon foundPlayer =
            other.GetComponentInParent<PlayerWeapon>();

        if (foundPlayer != null)
        {
            playerInRange = true;
            playerWeapon = foundPlayer;

            Debug.Log("Press E to pick up weapon");
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerWeapon foundPlayer =
            other.GetComponentInParent<PlayerWeapon>();

        if (foundPlayer == playerWeapon)
        {
            playerInRange = false;
            playerWeapon = null;
        }
    }
}