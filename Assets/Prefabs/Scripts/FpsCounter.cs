using UnityEngine;
using UnityEngine.UI;

// Shows how many frames per second the game is really making, in a screen corner.
// By default it also turns off VSync and the frame cap, so the number is what the PC can do
// rather than the monitor's refresh rate (60 on a 60 Hz screen).
public class FpsCounter : MonoBehaviour
{
    [Tooltip("Seconds of frames averaged together. Shorter reacts faster but jumps around more.")]
    public float averageOver = 0.5f;
    [Tooltip("Seconds between text updates, so the number is readable.")]
    public float refreshInterval = 0.25f;
    [Tooltip("Turns VSync off and removes the frame rate cap when the game starts. Off = the game runs at the monitor's rate (or the quality setting's).")]
    public bool uncapFrameRate = true;

    [Header("Look")]
    public int fontSize = 22;
    public Color goodColor = new Color(0.55f, 1f, 0.55f, 1f);
    public Color okColor = new Color(1f, 0.85f, 0.4f, 1f);
    public Color badColor = new Color(1f, 0.45f, 0.4f, 1f);

    public FpsSampler Sampler { get; private set; }

    private Text label;
    private float nextRefresh;

    void Awake()
    {
        Sampler = new FpsSampler(averageOver);

        label = GetComponent<Text>();
        if (label == null)
        {
            label = gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.alignment = TextAnchor.LowerRight;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        }
        label.fontSize = fontSize;
    }

    void Start()
    {
        if (uncapFrameRate) Uncap();
    }

    // With VSync on, Unity waits for the monitor before showing each frame, so the game can never
    // go faster than the refresh rate. targetFrameRate -1 means "no cap of our own"
    public static void Uncap()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
    }

    // Back to waiting for the monitor: smoother, and saves power
    public static void Cap()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
    }

    void Update()
    {
        // Real time, so pausing (time scale 0) doesn't break the counter
        Sampler.AddFrame(Time.unscaledDeltaTime);

        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + refreshInterval;

        label.text = Mathf.RoundToInt(Sampler.Fps) + " FPS\n"
            + Sampler.FrameMs.ToString("0.0") + " ms  (worst " + Sampler.WorstFrameMs.ToString("0") + ")";
        label.color = Sampler.Fps >= 60f ? goodColor : Sampler.Fps >= 30f ? okColor : badColor;
    }
}
