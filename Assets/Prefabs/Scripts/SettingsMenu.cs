using UnityEngine;
using UnityEngine.UI;

// The settings panel shared by the start menu and the pause menu.
// It hides whatever opened it and brings that back when it closes.
public class SettingsMenu : MonoBehaviour
{
    private const string VolumeKey = "MasterVolume";

    [Header("Controls")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("Navigation")]
    [Tooltip("Hidden while the settings are open and shown again when they close.")]
    public GameObject returnTo;

    // The panel starts hidden, so the saved volume is applied here instead of waiting for it to be opened
    [RuntimeInitializeOnLoadMethod]
    static void ApplySavedVolume()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
    }

    void OnEnable()
    {
        // Show the current values without triggering the change callbacks
        volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    public void Open()
    {
        if (returnTo != null) returnTo.SetActive(false);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        if (returnTo != null) returnTo.SetActive(true);
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume);
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
    }
}
