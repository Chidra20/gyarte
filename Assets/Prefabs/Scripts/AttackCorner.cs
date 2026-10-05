using UnityEngine;
using UnityEngine.UI;

// HUD corner showing the player's two attacks: whether each one can be used, how long until it can,
// and why not. Builds its own slots at start, so the prefab is just this component on a canvas child.
public class AttackCorner : MonoBehaviour
{
    [Header("Player (found by the Player tag when empty)")]
    public PlayerAttack attack;

    [Header("Look")]
    public Vector2 slotSize = new Vector2(110f, 110f);
    public float spacing = 10f;
    public Color slotColor = new Color(0.08f, 0.08f, 0.1f, 0.85f);
    [Tooltip("Covers the slot while the attack is unavailable, shrinking as it comes back.")]
    public Color cooldownColor = new Color(0f, 0f, 0f, 0.65f);
    public Color readyColor = new Color(0.55f, 1f, 0.55f, 1f);
    public Color blockedColor = new Color(1f, 0.6f, 0.45f, 1f);
    public int fontSize = 18;

    class Slot
    {
        public Image cover;
        public Text status;
        public Text extra;
    }

    private Slot swingSlot;
    private Slot fireSlot;
    private Font font;

    // "" → "Ready"; "Busy" → "Busy"; anything else gets the seconds left, e.g. "No charges 2.3s"
    public static string StatusText(string reason, float seconds)
    {
        if (string.IsNullOrEmpty(reason)) return "Ready";
        if (reason == "Busy") return reason;
        return reason + " " + seconds.ToString("0.0") + "s";
    }

    void Start()
    {
        if (attack == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) attack = player.GetComponent<PlayerAttack>();
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        swingSlot = BuildSlot("Swing", 0);
        fireSlot = BuildSlot("Fire", 1);
    }

    // Unscaled reads only, so the corner stays correct while the game is paused
    void Update()
    {
        if (attack == null) return;

        string meleeReason = attack.MeleeBlockedReason;
        Show(swingSlot, meleeReason, attack.MeleeCooldownRemaining, meleeReason == "" ? 0f : attack.MeleeCooldownFraction);
        swingSlot.extra.text = "";

        string spellReason = attack.SpellBlockedReason;
        float spellCover = attack.SpellCharges > 0 ? 0f : 1f - attack.RechargeProgress;
        if (spellReason == "Busy") spellCover = 1f;
        Show(fireSlot, spellReason, attack.SecondsToNextCharge, spellCover);
        fireSlot.extra.text = ChargeDots(attack.SpellCharges, attack.maxSpellCharges);
    }

    void Show(Slot slot, string reason, float seconds, float cover)
    {
        slot.cover.fillAmount = cover;
        slot.status.text = StatusText(reason, seconds);
        slot.status.color = reason == "" ? readyColor : blockedColor;
    }

    static string ChargeDots(int charges, int max)
    {
        var dots = new System.Text.StringBuilder();
        for (int i = 0; i < max; i++) dots.Append(i < charges ? "●" : "○");
        return dots.ToString();
    }

    Slot BuildSlot(string label, int index)
    {
        RectTransform root = NewRect(label, transform);
        root.anchorMin = root.anchorMax = root.pivot = Vector2.zero;
        root.sizeDelta = slotSize;
        root.anchoredPosition = new Vector2(index * (slotSize.x + spacing), 0f);
        root.gameObject.AddComponent<Image>().color = slotColor;

        var slot = new Slot();

        RectTransform coverRect = NewRect("Cooldown", root);
        Stretch(coverRect);
        slot.cover = coverRect.gameObject.AddComponent<Image>();
        slot.cover.color = cooldownColor;
        slot.cover.sprite = WhiteSprite();
        slot.cover.type = Image.Type.Filled;
        slot.cover.fillMethod = Image.FillMethod.Vertical;
        slot.cover.fillOrigin = (int)Image.OriginVertical.Bottom;
        slot.cover.fillAmount = 0f;

        Text name = NewText("Name", root, label, fontSize + 4, TextAnchor.UpperCenter);
        name.rectTransform.offsetMax = new Vector2(0f, -6f);
        slot.extra = NewText("Charges", root, "", fontSize, TextAnchor.MiddleCenter);
        slot.status = NewText("Status", root, "", fontSize - 2, TextAnchor.LowerCenter);
        slot.status.rectTransform.offsetMin = new Vector2(2f, 6f);
        slot.status.rectTransform.offsetMax = new Vector2(-2f, 0f);
        return slot;
    }

    Text NewText(string name, Transform parent, string value, int size, TextAnchor anchor)
    {
        RectTransform rect = NewRect(name, parent);
        Stretch(rect);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.raycastTarget = false;
        return text;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static Sprite whiteSprite;
    // A filled Image needs a sprite to fill
    static Sprite WhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return whiteSprite;
    }
}
