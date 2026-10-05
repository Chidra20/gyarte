using UnityEngine;
using UnityEngine.SceneManagement;

// Buttons of the start scene: start the game, open the settings, quit.
public class MainMenu : MonoBehaviour
{
    [Tooltip("Scene that Start loads. It has to be in the Build Settings scene list.")]
    public string gameSceneName = "Demo";
    public SettingsMenu settings;

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        settings.Open();
    }

    public void QuitGame()
    {
        // Application.Quit does nothing inside the editor, so stop Play mode there instead
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
