using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public Slider healthSlider;
    public Image healthFill;

    [Header("Low Health Glow")]
    public float lowHealthThreshold = 50f;
    public float glowSpeed = 6f;

    [Range(0f, 1f)]
    public float minimumGlowAlpha = 0.35f;

    private Color normalColor;

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        // Automatically find the Slider's Fill image
        // if you forgot to assign it.
        if (healthFill == null && healthSlider != null)
        {
            if (healthSlider.fillRect != null)
                healthFill =
                    healthSlider.fillRect.GetComponent<Image>();
        }

        if (healthFill != null)
        {
            normalColor = healthFill.color;
        }
        else
        {
            Debug.LogError(
                "HEALTH BAR: Health Fill could not be found!"
            );
        }
    }

    void Update()
    {
        if (playerHealth == null ||
            healthSlider == null)
            return;

        healthSlider.maxValue =
            playerHealth.MaxHealth;

        healthSlider.value =
            playerHealth.CurrentHealth;

        UpdateLowHealthGlow();
    }

    void UpdateLowHealthGlow()
    {
        if (healthFill == null)
            return;

        bool lowHealth =
            playerHealth.CurrentHealth < lowHealthThreshold &&
            playerHealth.CurrentHealth > 0f;

        if (lowHealth)
        {
            float pulse =
                (Mathf.Sin(Time.unscaledTime * glowSpeed) + 1f)
                * 0.5f;

            Color glowColor = normalColor;

            glowColor.a =
                Mathf.Lerp(
                    minimumGlowAlpha,
                    normalColor.a,
                    pulse
                );

            healthFill.color = glowColor;
        }
        else
        {
            healthFill.color = normalColor;
        }
    }
}