using UnityEngine;

// A soft glow around a flame that breathes in size and brightness like firelight. It is only a sprite
// (no real 2D light), so it costs next to nothing.
public class GlowFlicker : MonoBehaviour
{
    public SpriteRenderer glow;
    [Tooltip("How much the size and brightness wander (0 = steady).")]
    [Range(0f, 0.5f)] public float amount = 0.15f;
    public float speed = 3f;

    private Vector3 baseScale;
    private Color baseColor;
    private float seed;

    void Awake()
    {
        if (glow == null) glow = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        if (glow != null) baseColor = glow.color;
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Perlin noise wanders smoothly, unlike a sine wave, so it reads as fire rather than a pulse
        float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
        transform.localScale = baseScale * (1f + n * amount);
        if (glow != null)
        {
            Color c = baseColor;
            c.a = Mathf.Clamp01(baseColor.a * (1f + n * amount * 2f));
            glow.color = c;
        }
    }
}
