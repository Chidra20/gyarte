using UnityEngine;

// Hurts the player while this enemy touches them, at most once per damageInterval, and knocks them back.
// Contact is checked by overlap every frame rather than with trigger callbacks: slimes are kinematic
// and no longer collide with the player at all (the Player and Enemy layers ignore each other).
public class ContactDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    [Tooltip("Seconds between two hits while the player stays in contact.")]
    public float damageInterval = 1f;
    [Tooltip("How far from this enemy's centre the player counts as touching it.")]
    public float contactRadius = 0.55f;
    [Tooltip("Layer the player is on.")]
    public LayerMask playerLayers;

    [Header("Knockback")]
    public float knockbackSpeed = 8f;
    public float knockbackTime = 0.15f;

    private float lastHitTime = float.NegativeInfinity;
    private readonly Collider2D[] hits = new Collider2D[4];
    private ContactFilter2D filter;

    public static bool ReadyToHit(float lastHitTime, float now, float interval)
    {
        return now - lastHitTime >= interval;
    }

    void Awake()
    {
        if (playerLayers.value == 0) playerLayers = LayerMask.GetMask("Player");
        filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = playerLayers };
    }

    void Update()
    {
        if (!ReadyToHit(lastHitTime, Time.time, damageInterval)) return;

        int count = Physics2D.OverlapCircle(transform.position, contactRadius, filter, hits);
        for (int i = 0; i < count; i++)
        {
            if (!hits[i].CompareTag("Player")) continue;

            Health health = hits[i].GetComponent<Health>();
            if (health == null || !health.TakeDamage(damage)) continue;

            lastHitTime = Time.time;

            PlayerMovement movement = hits[i].GetComponent<PlayerMovement>();
            if (movement != null)
            {
                Vector2 away = (Vector2)(hits[i].transform.position - transform.position);
                if (away.sqrMagnitude < 0.0001f) away = Vector2.up;
                movement.ApplyKnockback(away.normalized * knockbackSpeed, knockbackTime);
            }
            return;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, contactRadius);
    }
}
