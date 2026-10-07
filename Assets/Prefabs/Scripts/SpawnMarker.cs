using UnityEngine;

// The glowing spot that warns an enemy is about to appear. Pulses in size and brightness;
// the wave spawner removes it when the enemy arrives.
public class SpawnMarker : MonoBehaviour
{
    public SpriteRenderer glow;
    [Tooltip("Pulses per second.")]
    public float pulseRate = 2.5f;
    [Tooltip("Size at the bottom and top of each pulse, as a multiple of the prefab's scale.")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.15f);
    [Tooltip("Brightness (alpha) at the bottom and top of each pulse.")]
    public Vector2 alphaRange = new Vector2(0.45f, 1f);

    private Vector3 baseScale;
    private Color baseColor;

    void Awake()
    {
        if (glow == null) glow = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        if (glow != null) baseColor = glow.color;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseRate * Mathf.PI * 2f) + 1f) / 2f;
        transform.localScale = baseScale * Mathf.Lerp(scaleRange.x, scaleRange.y, t);
        if (glow != null)
        {
            Color color = baseColor;
            color.a = baseColor.a * Mathf.Lerp(alphaRange.x, alphaRange.y, t);
            glow.color = color;
        }
    }
}
