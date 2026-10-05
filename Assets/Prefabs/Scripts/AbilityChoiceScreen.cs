using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Freezes the game and offers a few abilities as cards; picking one closes the screen and reports the pick.
// Builds its own cards each time it opens, so the prefab is just this component on a stretched canvas child.
public class AbilityChoiceScreen : MonoBehaviour
{
    [Header("Look")]
    public Color backdropColor = new Color(0f, 0f, 0f, 0.75f);
    public Color cardColor = new Color(0.14f, 0.13f, 0.17f, 1f);
    public Color cardHighlightColor = new Color(0.32f, 0.28f, 0.42f, 1f);
    public Vector2 cardSize = new Vector2(320f, 220f);
    public float cardSpacing = 40f;
    public string title = "Choose an ability";

    public bool IsOpen { get; private set; }

    private Action<Ability> onPicked;
    private Font font;
    private RectTransform content;

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Hide();
    }

    public void Open(IList<Ability> cards, Action<Ability> picked)
    {
        onPicked = picked;
        IsOpen = true;
        Time.timeScale = 0f;
        Build(cards);
    }

    // Hides the screen without a pick, e.g. when the player dies while it is open. Leaves the time scale alone
    public void Close()
    {
        onPicked = null;
        Hide();
    }

    void Pick(Ability ability)
    {
        if (!IsOpen) return;
        Action<Ability> callback = onPicked;
        Hide();
        Time.timeScale = 1f;
        callback?.Invoke(ability);
    }

    void Hide()
    {
        IsOpen = false;
        if (content != null) Destroy(content.gameObject);
        content = null;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Build(IList<Ability> cards)
    {
        if (content != null) Destroy(content.gameObject);

        content = NewRect("Choice", transform);
        Stretch(content);
        content.gameObject.AddComponent<Image>().color = backdropColor;

        Text heading = NewText("Title", content, title, 44, TextAnchor.MiddleCenter);
        heading.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        heading.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        heading.rectTransform.sizeDelta = new Vector2(0f, 60f);
        heading.rectTransform.anchoredPosition = new Vector2(0f, cardSize.y / 2f + 70f);

        float totalWidth = cards.Count * cardSize.x + (cards.Count - 1) * cardSpacing;
        Button first = null;
        for (int i = 0; i < cards.Count; i++)
        {
            Ability ability = cards[i];
            RectTransform card = NewRect(ability.displayName, content);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = cardSize;
            card.anchoredPosition = new Vector2(-totalWidth / 2f + cardSize.x / 2f + i * (cardSize.x + cardSpacing), 0f);

            Image image = card.gameObject.AddComponent<Image>();
            image.color = Color.white;
            Button button = card.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = cardColor;
            colors.highlightedColor = cardHighlightColor;
            colors.selectedColor = cardHighlightColor;
            colors.pressedColor = cardHighlightColor * 1.2f;
            button.colors = colors;
            button.onClick.AddListener(() => Pick(ability));

            Text name = NewText("Name", card, ability.displayName, 32, TextAnchor.UpperCenter);
            name.rectTransform.offsetMin = new Vector2(16f, 16f);
            name.rectTransform.offsetMax = new Vector2(-16f, -20f);
            Text description = NewText("Description", card, ability.description, 22, TextAnchor.MiddleCenter);
            description.rectTransform.offsetMin = new Vector2(20f, 16f);
            description.rectTransform.offsetMax = new Vector2(-20f, -40f);

            if (first == null) first = button;
        }

        // Gamepads need something selected to move from
        if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
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
}
