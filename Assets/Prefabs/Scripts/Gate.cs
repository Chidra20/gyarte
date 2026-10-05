using System;
using UnityEngine;

// The way out of a level. Walking into it with a key uses the key and opens it;
// walking into it once it is open finishes the level.
[RequireComponent(typeof(Collider2D))]
public class Gate : MonoBehaviour
{
    [Header("Look")]
    public SpriteRenderer sprite;
    public Color closedColor = new Color(0.25f, 0.22f, 0.22f, 1f);
    public Color openColor = new Color(0.95f, 0.85f, 0.45f, 1f);

    [Tooltip("Seconds after opening before standing in the gate counts as going through, so opening and leaving are two steps.")]
    public float enterDelay = 0.3f;

    public bool IsOpen { get; private set; }

    public event Action Opened;
    public event Action Entered;
    // Touched while closed with no key
    public event Action NeedsKey;

    private float openedTime;
    private bool entered;

    void Awake()
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        UpdateLook();
    }

    // Opens without a key, for the Test Menu
    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        openedTime = Time.time;
        UpdateLook();
        Opened?.Invoke();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!IsOpen)
        {
            Inventory inventory = other.GetComponent<Inventory>();
            if (inventory != null && inventory.TryUseKey()) Open();
            else NeedsKey?.Invoke();
            return;
        }

        TryEnter();
    }

    // The player is usually still standing in the gate when it opens, so staying inside also counts
    void OnTriggerStay2D(Collider2D other)
    {
        if (IsOpen && other.CompareTag("Player")) TryEnter();
    }

    void TryEnter()
    {
        if (entered || Time.time < openedTime + enterDelay) return;
        entered = true;
        Entered?.Invoke();
    }

    void UpdateLook()
    {
        if (sprite != null) sprite.color = IsOpen ? openColor : closedColor;
    }
}
