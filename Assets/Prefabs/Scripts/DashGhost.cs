using UnityEngine;

// One afterimage left behind by the dash: fades out, then removes itself
public class DashGhost : MonoBehaviour
{
    public float lifetime = 0.2f;

    private SpriteRenderer spriteRenderer;
    private float startAlpha;
    private float age;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startAlpha = spriteRenderer != null ? spriteRenderer.color.a : 1f;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(startAlpha, 0f, age / lifetime);
            spriteRenderer.color = color;
        }
        if (age >= lifetime) Destroy(gameObject);
    }
}
