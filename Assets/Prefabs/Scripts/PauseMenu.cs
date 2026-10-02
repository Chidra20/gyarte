using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Escape freezes the game and shows the pause menu; Escape again goes back one step.
// Lives on an object that is always active, because the menu itself starts hidden.
public class PauseMenu : MonoBehaviour
{
    [Tooltip("The whole pause screen. Hidden while the game is running.")]
    public GameObject pausePanel;
    public SettingsMenu settings;
    [Tooltip("Scene that End Game goes back to. It has to be in the Build Settings scene list.")]
    public string menuSceneName = "Start Menu";

    private bool isPaused;

    void Start()
    {
        pausePanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (!isPaused) Pause();
        else if (settings.gameObject.activeSelf) settings.Close();
        else Resume();
    }

    public void Pause()
    {
        // Freeze the game first, then show the menu
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (settings.gameObject.activeSelf) settings.Close();

        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void OpenSettings()
    {
        settings.Open();
    }

    public void EndGame()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    void OnDestroy()
    {
        // Time scale survives scene changes, so never leave the next scene frozen
        Time.timeScale = 1f;
    }
}
