using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaBar : MonoBehaviour
{
    [Header("References")]
    public PlayerStamina playerStamina;
    public Slider staminaSlider;
    public Image staminaFill;

    [Header("Exhausted Glow")]
    public float glowDuration = 0.6f;
    public float glowSpeed = 12f;
    public float glowBrightness = 2f;

    private Color normalColor;
    private float glowTimer;

    void Start()
    {
        if (playerStamina == null)
            playerStamina = FindFirstObjectByType<PlayerStamina>();

        if (staminaSlider == null)
            staminaSlider = GetComponent<Slider>();

        if (staminaFill != null)
            normalColor = staminaFill.color;

        if (playerStamina == null)
            Debug.LogError("STAMINA BAR: PlayerStamina not found!");

        if (staminaSlider == null)
            Debug.LogError("STAMINA BAR: Slider not found!");

        if (staminaFill == null)
            Debug.LogError("STAMINA BAR: Stamina Fill is not assigned!");
    }

    void Update()
    {
        if (playerStamina != null &&
            staminaSlider != null)
        {
            staminaSlider.maxValue =
                playerStamina.MaxStamina;

            staminaSlider.value =
                playerStamina.CurrentStamina;
        }

        UpdateGlow();
    }

    public void FlashNotEnoughStamina()
    {
        glowTimer = glowDuration;
    }

    void UpdateGlow()
    {
        if (staminaFill == null)
            return;

        if (glowTimer > 0f)
        {
            glowTimer -= Time.deltaTime;

            float pulse =
                (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.5f;

            float brightness =
                Mathf.Lerp(1f, glowBrightness, pulse);

            staminaFill.color =
                normalColor * brightness;
        }
        else
        {
            staminaFill.color = normalColor;
        }
    }
}