using System.Collections.Generic;
using UnityEngine;

// Keeps rooms the player has never been in dark. Once a room has been entered its darkness
// fades away and it stays lit, so the explored part of the level stays visible.
public class RoomManager : MonoBehaviour
{
    [Tooltip("Seconds for a room's darkness to fade out when it is entered.")]
    public float fadeTime = 0.35f;

    private Transform player;
    // Tracked by object, not by name, so a freshly generated room is never mistaken for the old one
    private Transform currentRoom;
    private readonly HashSet<Transform> visitedRooms = new HashSet<Transform>();

    // Per darkness overlay: its full (dark) alpha, how dark it is right now, and where it is heading
    private readonly Dictionary<SpriteRenderer, float> fullAlpha = new Dictionary<SpriteRenderer, float>();
    private readonly Dictionary<SpriteRenderer, float> currentAlpha = new Dictionary<SpriteRenderer, float>();
    private readonly Dictionary<SpriteRenderer, float> targetAlpha = new Dictionary<SpriteRenderer, float>();

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("No GameObject with the 'Player' tag found.");
        }
    }

    void Update()
    {
        if (player != null)
        {
            foreach (Transform room in transform)
            {
                BoxCollider2D roomCollider = room.GetComponent<BoxCollider2D>();

                if (roomCollider != null && roomCollider.OverlapPoint(player.position))
                {
                    if (currentRoom != room)
                    {
                        currentRoom = room;
                        visitedRooms.Add(room);
                        RefreshDarkness();
                    }

                    break;
                }
            }
        }

        AnimateFades();
    }

    // True when the point is in a room the player has already explored, or not in any room at all.
    // Enemies in an unexplored (dark) room use this to know they can't see the player.
    public bool IsRevealed(Vector2 point)
    {
        foreach (Transform room in transform)
        {
            BoxCollider2D roomCollider = room.GetComponent<BoxCollider2D>();
            if (roomCollider != null && roomCollider.OverlapPoint(point))
                return visitedRooms.Contains(room);
        }
        return true;
    }

    // Lights every room that has been visited and darkens the rest.
    // instant: jump straight there instead of fading (used by the Test Menu).
    public void RefreshDarkness(bool instant = false)
    {
        // Rooms from an old level are destroyed when a new one is generated
        visitedRooms.RemoveWhere(room => room == null);
        Forget(r => r == null);

        foreach (Transform room in transform)
        {
            SpriteRenderer overlay = room.GetComponent<SpriteRenderer>();
            if (overlay == null) continue;

            Track(overlay);
            float target = visitedRooms.Contains(room) ? 0f : fullAlpha[overlay];
            targetAlpha[overlay] = target;
            if (instant) SetAlpha(overlay, target);
        }
    }

    void AnimateFades()
    {
        if (targetAlpha.Count == 0) return;

        var overlays = new List<SpriteRenderer>(targetAlpha.Keys);
        foreach (SpriteRenderer overlay in overlays)
        {
            if (overlay == null) continue;

            float current = currentAlpha[overlay];
            float target = targetAlpha[overlay];
            if (Mathf.Approximately(current, target)) continue;

            float speed = fadeTime > 0f ? fullAlpha[overlay] / fadeTime : float.MaxValue;
            SetAlpha(overlay, Mathf.MoveTowards(current, target, speed * Time.deltaTime));
        }
    }

    // Remember an overlay the first time it is seen. A hidden overlay counts as fully faded out.
    void Track(SpriteRenderer overlay)
    {
        if (fullAlpha.ContainsKey(overlay)) return;

        fullAlpha[overlay] = overlay.color.a > 0f ? overlay.color.a : 1f;
        currentAlpha[overlay] = overlay.enabled ? overlay.color.a : 0f;
        targetAlpha[overlay] = currentAlpha[overlay];
    }

    void SetAlpha(SpriteRenderer overlay, float alpha)
    {
        currentAlpha[overlay] = alpha;
        Color color = overlay.color;
        color.a = alpha;
        overlay.color = color;
        overlay.enabled = alpha > 0.001f;
    }

    void Forget(System.Predicate<SpriteRenderer> match)
    {
        foreach (var overlay in new List<SpriteRenderer>(fullAlpha.Keys))
        {
            if (!match(overlay)) continue;
            fullAlpha.Remove(overlay);
            currentAlpha.Remove(overlay);
            targetAlpha.Remove(overlay);
        }
    }
}
