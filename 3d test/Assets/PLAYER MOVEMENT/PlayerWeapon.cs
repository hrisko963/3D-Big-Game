using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Weapon Holders")]
    public Transform weaponHolder;
    public Transform hipWeaponHolder;

    [Header("Current Weapon")]
    public GameObject equippedWeapon;

    [Header("Animator")]
    public Animator animator;

    [Header("Draw / Sheathe")]
    public KeyCode toggleWeaponKey = KeyCode.R;

    private bool hasWeapon;
    private bool ownsWeapon;

    private SwordHitbox swordHitbox;

    private Vector3 handLocalPosition;
    private Quaternion handLocalRotation;
    private Vector3 handLocalScale;

    public Vector3 hipLocalPosition;
    public Vector3 hipLocalRotation;

    public bool HasWeapon
    {
        get { return hasWeapon; }
    }

    public bool OwnsWeapon
    {
        get { return ownsWeapon; }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (equippedWeapon != null)
        {
            equippedWeapon.SetActive(false);
        }

        UpdateAnimator();
    }

    void Update()
    {
        if (!ownsWeapon)
            return;

        if (Input.GetKeyDown(toggleWeaponKey))
        {
            ToggleWeapon();
        }
    }

    // =========================================
    // PICK UP SWORD
    // =========================================

    public void PickUpWeapon(GameObject pickupWeapon)
    {
        if (ownsWeapon)
            return;

        if (weaponHolder == null)
        {
            Debug.LogError(
                "PLAYER WEAPON: WeaponHolder is not assigned!"
            );

            return;
        }

        if (hipWeaponHolder == null)
        {
            Debug.LogError(
                "PLAYER WEAPON: HipWeaponHolder is not assigned!"
            );

            return;
        }

        if (equippedWeapon != null)
        {
            equippedWeapon.SetActive(true);
        }
        else
        {
            equippedWeapon = Instantiate(
                pickupWeapon,
                weaponHolder
            );

            WeaponPickup pickup =
                equippedWeapon.GetComponent<WeaponPickup>();

            if (pickup != null)
            {
                Destroy(pickup);
            }

            Rigidbody rb =
                equippedWeapon.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Destroy(rb);
            }

            equippedWeapon.transform.localPosition =
                Vector3.zero;

            equippedWeapon.transform.localRotation =
                Quaternion.identity;
        }

        equippedWeapon.transform.SetParent(
            weaponHolder,
            true
        );

        // Find our dedicated sword hitbox.
        swordHitbox =
            equippedWeapon.GetComponentInChildren<SwordHitbox>(
                true
            );

        // Disable ordinary weapon/pickup colliders.
        // SwordHitbox manages its own collider.
        Collider[] colliders =
            equippedWeapon.GetComponentsInChildren<Collider>(
                true
            );

        foreach (Collider col in colliders)
        {
            if (swordHitbox != null &&
                col == swordHitbox.hitboxCollider)
            {
                col.isTrigger = true;
                col.enabled = false;
            }
            else
            {
                col.enabled = false;
            }
        }

        handLocalPosition =
            equippedWeapon.transform.localPosition;

        handLocalRotation =
            equippedWeapon.transform.localRotation;

        handLocalScale =
            equippedWeapon.transform.localScale;

        ownsWeapon = true;
        hasWeapon = true;

        UpdateAnimator();

        Debug.Log(
            "SWORD PICKED UP AND EQUIPPED"
        );
    }

    // =========================================
    // GET SWORD HITBOX
    // =========================================

    public SwordHitbox GetSwordHitbox()
    {
        if (equippedWeapon == null)
            return null;

        if (swordHitbox == null)
        {
            swordHitbox =
                equippedWeapon.GetComponentInChildren<SwordHitbox>(
                    true
                );
        }

        return swordHitbox;
    }

    // =========================================
    // TOGGLE
    // =========================================

    void ToggleWeapon()
    {
        if (hasWeapon)
        {
            SheatheWeapon();
        }
        else
        {
            DrawWeapon();
        }
    }

    // =========================================
    // SHEATHE
    // =========================================

    void SheatheWeapon()
    {
        if (equippedWeapon == null)
            return;

        if (hipWeaponHolder == null)
            return;

        SwordHitbox hitbox =
            GetSwordHitbox();

        if (hitbox != null)
        {
            hitbox.EndAttack();
        }

        hasWeapon = false;

        equippedWeapon.transform.SetParent(
            hipWeaponHolder,
            false
        );

        equippedWeapon.transform.localPosition =
            hipLocalPosition;

        equippedWeapon.transform.localRotation =
            Quaternion.Euler(
                hipLocalRotation
            );

        equippedWeapon.transform.localScale =
            handLocalScale;

        UpdateAnimator();

        Debug.Log("SWORD SHEATHED");
    }

    // =========================================
    // DRAW
    // =========================================

    void DrawWeapon()
    {
        if (equippedWeapon == null)
            return;

        if (weaponHolder == null)
            return;

        equippedWeapon.transform.SetParent(
            weaponHolder,
            false
        );

        equippedWeapon.transform.localPosition =
            handLocalPosition;

        equippedWeapon.transform.localRotation =
            handLocalRotation;

        equippedWeapon.transform.localScale =
            handLocalScale;

        hasWeapon = true;

        UpdateAnimator();

        Debug.Log("SWORD DRAWN");
    }

    public void DrawWeaponIfOwned()
    {
        if (!ownsWeapon)
            return;

        if (hasWeapon)
            return;

        DrawWeapon();
    }

    // =========================================
    // ANIMATOR
    // =========================================

    void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetBool(
                "Armed",
                hasWeapon
            );
        }
    }
}