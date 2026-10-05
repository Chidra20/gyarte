using UnityEngine;

// Blinks the player's sprite while their Health is invulnerable after a hit,
// so it is clear that the next hit will not count yet.
[RequireComponent(typeof(Health))]
public class PlayerHurtFlash : MonoBehaviour
{
    [Tooltip("The sprite to blink. Found on the 'visual' child when left empty.")]
    public SpriteRenderer sprite;
    [Tooltip("Blinks per second.")]
    public float blinkRate = 12f;
    [Range(0f, 1f)] public float blinkAlpha = 0.3f;

    private Health health;
    private Color baseColor;

    void Awake()
    {
        health = GetComponent<Health>();
        if (sprite == null)
        {
            Transform visual = transform.Find("visual");
            if (visual != null) sprite = visual.GetComponent<SpriteRenderer>();
        }
        if (sprite != null) baseColor = sprite.color;
    }

    void Update()
    {
        if (sprite == null) return;

        Color color = baseColor;
        if (health.IsInvulnerable && !health.IsDead)
        {
            bool dim = Mathf.Repeat(Time.time * blinkRate, 1f) < 0.5f;
            if (dim) color.a = baseColor.a * blinkAlpha;
        }
        sprite.color = color;
    }
}
