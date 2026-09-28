using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class DeathScreen : MonoBehaviour
{
    [Header("UI")]
    public GameObject deathPanel;
    public Button respawnButton;
    public CanvasGroup canvasGroup;

    [Header("Death Screen Timing")]
    public float showDelay = 1.5f;
    public float fadeDuration = 1f;

    void Awake()
    {
        if (deathPanel != null)
            deathPanel.SetActive(false);

        if (respawnButton != null)
        {
            respawnButton.onClick.RemoveAllListeners();
            respawnButton.onClick.AddListener(Respawn);
        }

        if (canvasGroup == null && deathPanel != null)
            canvasGroup = deathPanel.GetComponent<CanvasGroup>();
    }

    public void ShowDeathScreen()
    {
        StartCoroutine(ShowDeathScreenRoutine());
    }

    IEnumerator ShowDeathScreenRoutine()
    {
        // Make sure mouse is available immediately after death.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Wait so the death animation can play first.
        yield return new WaitForSeconds(showDelay);

        if (deathPanel == null)
            yield break;

        deathPanel.SetActive(true);

        if (canvasGroup == null)
        {
            Debug.LogError("DEATH SCREEN: CanvasGroup is not assigned!");
            yield break;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            canvasGroup.alpha =
                Mathf.Clamp01(
                    timer / fadeDuration
                );

            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Respawn()
    {
        Debug.Log("RESPAWN BUTTON PRESSED!");

        Time.timeScale = 1f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.name
        );
    }
}