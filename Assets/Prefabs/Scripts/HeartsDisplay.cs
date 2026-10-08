using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shows a value as a row of hearts: the player's health, or their fireball charges (stamina).
// Each heart holds healthPerHeart points (2 by default, so a heart can be full, half or empty).
// Hearts are added or removed automatically when the maximum changes, e.g. from an ability.
public class HeartsDisplay : MonoBehaviour
{
    public enum Source { Health, SpellCharges }

    [Tooltip("What the hearts show: health, or fireball charges (each fireball uses one half heart).")]
    public Source source = Source.Health;
    [Tooltip("Whose health to show. The player (by tag) when empty.")]
    public Health health;
    [Tooltip("Whose fireball charges to show. The player (by tag) when empty.")]
    public PlayerAttack attack;

    [Header("Sprites")]
    public Sprite fullHeart;
    public Sprite halfHeart;
    public Sprite emptyHeart;
    public int healthPerHeart = 2;

    [Header("Layout")]
    [Tooltip("Size of one art pixel on screen (the canvas is 1920x1080).")]
    public float pixelScale = 5f;
    [Tooltip("Gap between hearts, in screen pixels.")]
    public float spacing = 4f;

    [Header("Hit Pop")]
    [Tooltip("A heart that changes briefly grows to this size, then settles back.")]
    public float popScale = 1.35f;
    public float popTime = 0.2f;

    private readonly List<Image> hearts = new List<Image>();
    private readonly List<float> popTimers = new List<float>();
    private readonly List<Sprite> shown = new List<Sprite>();
    private int lastCharges = -1, lastMaxCharges = -1;

    void Start()
    {
        if (source == Source.Health) TryConnect();
        else TryConnectAttack();
    }

    void OnDestroy()
    {
        if (health != null) health.Changed -= Show;
    }

    void Update()
    {
        // The player might not exist yet when the HUD starts
        if (source == Source.Health)
        {
            if (health == null) TryConnect();
        }
        else
        {
            if (attack == null) TryConnectAttack();
            // Charges have no change event, so check them every frame
            if (attack != null && (attack.SpellCharges != lastCharges || attack.maxSpellCharges != lastMaxCharges))
            {
                lastCharges = attack.SpellCharges;
                lastMaxCharges = attack.maxSpellCharges;
                Show(lastCharges, lastMaxCharges);
            }
        }

        // Unscaled, so the pop still finishes if the game gets paused right after a hit
        for (int i = 0; i < hearts.Count; i++)
        {
            if (popTimers[i] <= 0f) continue;
            popTimers[i] -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(popTimers[i] / popTime);
            hearts[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, popScale, t);
        }
    }

    void TryConnect()
    {
        if (health == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) health = player.GetComponent<Health>();
        }
        if (health == null) return;

        health.Changed -= Show;
        health.Changed += Show;
        Show(health.CurrentHealth, health.maxHealth);
    }

    void TryConnectAttack()
    {
        if (attack != null) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) attack = player.GetComponent<PlayerAttack>();
    }

    void Show(int current, int max)
    {
        int count = Mathf.Max(1, Mathf.CeilToInt(max / (float)healthPerHeart));
        while (hearts.Count < count) AddHeart();
        while (hearts.Count > count) RemoveLastHeart();

        for (int i = 0; i < hearts.Count; i++)
        {
            int value = Mathf.Clamp(current - i * healthPerHeart, 0, healthPerHeart);
            Sprite sprite = value >= healthPerHeart ? fullHeart : value > 0 ? halfHeart : emptyHeart;

            // Pop the hearts that just changed (not on the very first draw)
            if (shown[i] != null && shown[i] != sprite) popTimers[i] = popTime;

            hearts[i].sprite = sprite;
            shown[i] = sprite;
        }
    }

    void AddHeart()
    {
        int index = hearts.Count;
        var heartObject = new GameObject("Heart " + (index + 1), typeof(RectTransform), typeof(Image));
        heartObject.transform.SetParent(transform, false);

        Image image = heartObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;

        Vector2 size = fullHeart != null ? fullHeart.rect.size * pixelScale : new Vector2(50f, 50f);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(index * (size.x + spacing) + size.x / 2f, -size.y / 2f);

        hearts.Add(image);
        popTimers.Add(0f);
        shown.Add(null);
    }

    void RemoveLastHeart()
    {
        int last = hearts.Count - 1;
        Destroy(hearts[last].gameObject);
        hearts.RemoveAt(last);
        popTimers.RemoveAt(last);
        shown.RemoveAt(last);
    }
}
