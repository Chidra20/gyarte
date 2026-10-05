using UnityEngine;
using UnityEngine.UI;

// Shows the player's health as "HP 7/10". Needs a Text on the same object (added if missing).
public class HealthLabel : MonoBehaviour
{
    [Tooltip("Whose health to show. The player (by tag) when empty.")]
    public Health health;
    public string prefix = "HP ";
    public Color normalColor = Color.white;
    [Tooltip("Colour once health is at or below lowFraction of the maximum.")]
    public Color lowColor = new Color(1f, 0.4f, 0.35f, 1f);
    [Range(0f, 1f)] public float lowFraction = 0.3f;

    private Text label;

    void Start()
    {
        label = GetComponent<Text>();
        if (label == null)
        {
            label = gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.raycastTarget = false;
        }

        if (health == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) health = player.GetComponent<Health>();
        }
        if (health == null) return;

        health.Changed += Show;
        Show(health.CurrentHealth, health.maxHealth);
    }

    void OnDestroy()
    {
        if (health != null) health.Changed -= Show;
    }

    void Show(int current, int max)
    {
        label.text = prefix + current + "/" + max;
        label.color = current <= max * lowFraction ? lowColor : normalColor;
    }
}
