using System;
using UnityEngine;

// A field of floor spikes that keeps cycling: hidden, rising, out, retracting. While fully out it hurts
// whoever stands on it, the player and slimes alike, at most once per hitInterval. All the spikes of a
// field share one renderer list and one clock, so they move together.
public class SpikeTrap : MonoBehaviour
{
    public enum Phase { Hidden, Rising, Out, Retracting }

    [Serializable]
    public struct Timings
    {
        public float hidden;
        public float rising;
        public float outTime;
        public float retracting;
        public float Cycle => hidden + rising + outTime + retracting;
    }

    [Header("Look")]
    [Tooltip("The spike animation: frame 0 is holes only, the last frame fully out.")]
    public Sprite[] frames;
    [Tooltip("Every spike of this field.")]
    public SpriteRenderer[] spikes;

    [Header("Timing (seconds)")]
    public Timings timings = new Timings { hidden = 2f, rising = 0.25f, outTime = 1.2f, retracting = 0.25f };

    [Header("Damage")]
    public int damage = 1;
    [Tooltip("Seconds between two hits on whoever stays on the spikes while they are out.")]
    public float hitInterval = 0.6f;
    [Tooltip("Size of the dangerous area (the field's cells).")]
    public Vector2 areaSize = Vector2.one;
    [Tooltip("Centre of the dangerous area, from this object (its pivot is at the bottom of the field).")]
    public Vector2 areaOffset;
    public LayerMask targets;

    private float clockOffset;
    private float nextHit;
    private readonly Collider2D[] hits = new Collider2D[8];

    public static Phase PhaseAt(float time, Timings t)
    {
        float m = Mathf.Repeat(time, t.Cycle);
        if (m < t.hidden) return Phase.Hidden;
        m -= t.hidden;
        if (m < t.rising) return Phase.Rising;
        m -= t.rising;
        if (m < t.outTime) return Phase.Out;
        return Phase.Retracting;
    }

    public static bool IsDangerous(Phase phase)
    {
        return phase == Phase.Out;
    }

    // Which animation frame to show: 0 while hidden, the last while out, the ones between while moving
    public static int FrameAt(float time, Timings t, int frameCount)
    {
        if (frameCount <= 1) return 0;
        Phase phase = PhaseAt(time, t);
        if (phase == Phase.Hidden) return 0;
        if (phase == Phase.Out) return frameCount - 1;

        int middle = Mathf.Max(1, frameCount - 2);
        float m = Mathf.Repeat(time, t.Cycle);
        if (phase == Phase.Rising)
        {
            float progress = (m - t.hidden) / Mathf.Max(0.0001f, t.rising);
            return Mathf.Clamp(1 + Mathf.FloorToInt(progress * middle), 1, frameCount - 2);
        }
        float back = (m - t.hidden - t.rising - t.outTime) / Mathf.Max(0.0001f, t.retracting);
        return Mathf.Clamp(frameCount - 2 - Mathf.FloorToInt(back * middle), 1, frameCount - 2);
    }

    void Awake()
    {
        // Different fields start at different points of the cycle so they don't all snap together
        clockOffset = UnityEngine.Random.Range(0f, timings.Cycle);
        if (targets.value == 0) targets = LayerMask.GetMask("Player", "Enemy");
    }

    void Update()
    {
        float time = Time.time + clockOffset;

        if (frames != null && frames.Length > 0)
        {
            Sprite sprite = frames[FrameAt(time, timings, frames.Length)];
            foreach (SpriteRenderer spike in spikes) if (spike != null) spike.sprite = sprite;
        }

        if (!IsDangerous(PhaseAt(time, timings)) || Time.time < nextHit) return;

        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = targets };
        int count = Physics2D.OverlapBox((Vector2)transform.position + areaOffset, areaSize * 0.9f, 0f, filter, hits);
        bool hitSomething = false;
        for (int i = 0; i < count; i++)
        {
            EnemyHealth enemy = hits[i].GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                hitSomething = true;
                continue;
            }
            Health health = hits[i].GetComponent<Health>();
            if (health != null && hits[i].CompareTag("Player"))
            {
                // The player's hurt immunity decides whether this one counts
                health.TakeDamage(damage);
                hitSomething = true;
            }
        }
        if (hitSomething) nextHit = Time.time + hitInterval;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.DrawWireCube((Vector2)transform.position + areaOffset, areaSize);
    }
}
