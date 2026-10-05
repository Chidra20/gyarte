using System.Text;
using UnityEngine;
using UnityEngine.UI;

// The game loop's part of the HUD: level and wave, what to do next, the inventory, the level banner
// and the death screen. Builds its own texts at start, so the prefab is just this component on a stretched canvas child.
public class LoopHud : MonoBehaviour
{
    [Header("References (found automatically when empty)")]
    public GameLoop loop;
    public WaveSpawner spawner;
    public Inventory inventory;

    [Header("Look")]
    public int fontSize = 30;
    public Color textColor = Color.white;
    public Color warningColor = new Color(1f, 0.55f, 0.45f, 1f);
    [Tooltip("Seconds 'Need a key' shows after touching a closed gate without one.")]
    public float needKeyTime = 2f;

    private Text status;
    private Text objective;
    private Text inventoryText;
    private Text banner;
    private RectTransform deathScreen;
    private float bannerUntil;
    private float needKeyUntil;
    private Font font;

    public string BannerText => banner != null ? banner.text : "";

    // Public so EditMode tests can call it. The texts are built in Awake, not Start: GameLoop.Start
    // shows the first "Level 1" banner, and it may run before this script's Start
    public void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        status = NewText("Level and Wave", new Vector2(0.5f, 1f), new Vector2(0f, -20f), fontSize, TextAnchor.UpperCenter);
        objective = NewText("Objective", new Vector2(0.5f, 1f), new Vector2(0f, -60f), fontSize - 4, TextAnchor.UpperCenter);
        inventoryText = NewText("Inventory", new Vector2(1f, 1f), new Vector2(-24f, -20f), fontSize - 6, TextAnchor.UpperRight);
        banner = NewText("Banner", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), fontSize * 2, TextAnchor.MiddleCenter);
        banner.text = "";
        BuildDeathScreen();
    }

    void Start()
    {
        if (loop == null) loop = FindAnyObjectByType<GameLoop>();
        if (spawner == null) spawner = FindAnyObjectByType<WaveSpawner>();
        if (inventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) inventory = player.GetComponent<Inventory>();
        }
    }

    // Shown over the game for 'seconds' of real time
    public void ShowBanner(string text, float seconds)
    {
        if (banner == null) return;
        banner.text = text;
        bannerUntil = Time.unscaledTime + seconds;
    }

    public void ShowNeedKey()
    {
        needKeyUntil = Time.unscaledTime + needKeyTime;
    }

    public void ShowDeath()
    {
        if (deathScreen != null) deathScreen.gameObject.SetActive(true);
    }

    // Unscaled time throughout, so the HUD keeps working while the game is frozen
    void Update()
    {
        if (loop == null) return;

        if (banner.text != "" && Time.unscaledTime > bannerUntil) banner.text = "";

        // Only while fighting: between levels the spawner still holds the last level's numbers
        string wave = spawner != null && spawner.TotalWaves > 0 && loop.State == LoopState.Fighting ?"  ·  Wave " + Mathf.Max(1, spawner.CurrentWave) + "/" + spawner.TotalWaves : "";
        status.text = "Level " + loop.Level + wave;

        bool needKey = Time.unscaledTime < needKeyUntil && loop.State != LoopState.GateOpen;
        objective.text = needKey ? "Need a key" : Objective();
        objective.color = needKey ? warningColor : textColor;

        inventoryText.text = InventoryLine();
    }

    string Objective()
    {
        switch (loop.State)
        {
            case LoopState.Fighting:
                float next = spawner != null ? spawner.NextWaveIn : 0f;
                return next > 0f ? "Next wave in " + Mathf.CeilToInt(next) + "s" : "Survive";
            case LoopState.KeyHunt:
                return inventory != null && inventory.Keys > 0 ? "Go to the gate" : "Find the key";
            case LoopState.GateOpen:
                return "Go through the gate";
            default:
                return "";
        }
    }

    string InventoryLine()
    {
        if (inventory == null) return "";
        var line = new StringBuilder();
        if (inventory.Keys > 0) line.Append("Key" + (inventory.Keys > 1 ? " x" + inventory.Keys : "")).Append('\n');
        foreach (var pair in inventory.Abilities)
        {
            line.Append(pair.Key.displayName);
            if (pair.Value > 1) line.Append(" x").Append(pair.Value);
            line.Append('\n');
        }
        return line.ToString();
    }

    void BuildDeathScreen()
    {
        deathScreen = NewRect("Death Screen", transform);
        deathScreen.anchorMin = Vector2.zero;
        deathScreen.anchorMax = Vector2.one;
        deathScreen.offsetMin = deathScreen.offsetMax = Vector2.zero;
        deathScreen.gameObject.AddComponent<Image>().color = new Color(0.15f, 0f, 0f, 0.8f);

        Text text = NewText("You Died", new Vector2(0.5f, 0.5f), Vector2.zero, fontSize * 3, TextAnchor.MiddleCenter);
        text.transform.SetParent(deathScreen, false);
        text.text = "You died";
        text.color = new Color(1f, 0.35f, 0.3f, 1f);
        deathScreen.gameObject.SetActive(false);
    }

    Text NewText(string name, Vector2 anchor, Vector2 position, int size, TextAnchor alignment)
    {
        RectTransform rect = NewRect(name, transform);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(900f, size * 6f);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = textColor;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        rect.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
}
