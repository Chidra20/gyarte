using System;
using UnityEngine;

// The way out of a level. Walking into it with a key uses the key and opens it;
// walking into it once it is open finishes the level.
[RequireComponent(typeof(Collider2D))]
public class Gate : MonoBehaviour
{
    [Header("Look")]
    public SpriteRenderer sprite;
    [Tooltip("Shown only while closed (e.g. the bars over the stairs, with their solid collider).")]
    public GameObject closedLook;
    [Tooltip("Shown only once open.")]
    public GameObject openLook;
    public Color closedColor = new Color(0.25f, 0.22f, 0.22f, 1f);
    public Color openColor = new Color(0.95f, 0.85f, 0.45f, 1f);

    [Tooltip("Seconds after opening before standing in the gate counts as going through, so opening and leaving are two steps.")]
    public float enterDelay = 0.3f;
    [Tooltip("If set, going through only counts with the player inside this area (centred on the gate plus the offset), e.g. on the stairs' steps rather than in front of them. Zero size = anywhere in the trigger.")]
    public Vector2 enterAreaSize;
    public Vector2 enterAreaOffset;

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

        TryEnter(other.transform.position);
    }

    // The player is usually still standing in the gate when it opens, so staying inside also counts
    void OnTriggerStay2D(Collider2D other)
    {
        if (IsOpen && other.CompareTag("Player")) TryEnter(other.transform.position);
    }

    public static bool InsideEnterArea(Vector2 player, Vector2 gate, Vector2 offset, Vector2 size)
    {
        if (size.x <= 0f || size.y <= 0f) return true;
        Vector2 local = player - (gate + offset);
        return Mathf.Abs(local.x) <= size.x / 2f && Mathf.Abs(local.y) <= size.y / 2f;
    }

    void TryEnter(Vector2 playerPosition)
    {
        if (entered || Time.time < openedTime + enterDelay) return;
        if (!InsideEnterArea(playerPosition, transform.position, enterAreaOffset, enterAreaSize)) return;
        entered = true;
        Entered?.Invoke();
    }

    void UpdateLook()
    {
        if (sprite != null) sprite.color = IsOpen ? openColor : closedColor;
        if (closedLook != null) closedLook.SetActive(!IsOpen);
        if (openLook != null) openLook.SetActive(IsOpen);
    }
}
