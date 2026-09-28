using UnityEngine;

public class RespawnButton : MonoBehaviour
{
    [Header("References")]
    public DeathScreen deathScreen;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (deathScreen == null)
            deathScreen = FindFirstObjectByType<DeathScreen>();

        if (rectTransform == null)
            Debug.LogError("RESPAWN BUTTON: RectTransform not found!");

        if (deathScreen == null)
            Debug.LogError("RESPAWN BUTTON: DeathScreen not found!");
    }

    void Update()
    {
        // Mouse click
        if (Input.GetMouseButtonDown(0))
        {
            bool mouseIsOverButton =
                RectTransformUtility.RectangleContainsScreenPoint(
                    rectTransform,
                    Input.mousePosition,
                    null
                );

            if (mouseIsOverButton)
            {
                Debug.Log("RESPAWN BUTTON CLICKED!");
                Respawn();
            }
        }

        // Keyboard backup
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("RESPAWN ENTER PRESSED!");
            Respawn();
        }
    }

    void Respawn()
    {
        if (deathScreen != null)
        {
            deathScreen.Respawn();
        }
    }
}