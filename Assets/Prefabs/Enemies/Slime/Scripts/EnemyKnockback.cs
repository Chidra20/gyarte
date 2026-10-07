using UnityEngine;

// Pushes an enemy back when it is hit. Slimes are kinematic and walls don't stop them physically,
// so the push checks for walls itself and stops at them. While it lasts the enemy's own movement pauses.
public class EnemyKnockback : MonoBehaviour
{
    [Tooltip("Seconds a hit pushes the enemy for. How hard it is pushed is set by the attack (PlayerAttack).")]
    public float duration = 0.15f;
    [Tooltip("Colliders with this tag stop the push.")]
    public string wallTag = "wall";

    public bool IsKnockedBack => Time.time < pushUntil;

    private Rigidbody2D rb;
    private float radius = 0.3f;
    private Vector2 velocity;
    private float pushUntil;
    private ContactFilter2D wallFilter = new ContactFilter2D { useTriggers = false };
    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    // Away from 'from' toward the enemy; 'fallback' when the two are on the same spot
    public static Vector2 Direction(Vector2 from, Vector2 enemy, Vector2 fallback)
    {
        Vector2 away = enemy - from;
        if (away.sqrMagnitude < 0.0001f) away = fallback;
        return away.sqrMagnitude < 0.0001f ? Vector2.zero : away.normalized;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Collider2D body = GetComponent<Collider2D>();
        if (body != null) radius = body.bounds.extents.x * 0.9f;
    }

    public void Push(Vector2 pushVelocity)
    {
        if (pushVelocity.sqrMagnitude < 0.0001f || duration <= 0f) return;
        velocity = pushVelocity;
        pushUntil = Time.time + duration;
    }

    void FixedUpdate()
    {
        if (!IsKnockedBack) return;

        Vector2 position = rb != null ? rb.position : (Vector2)transform.position;
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        Vector2 direction = velocity.normalized;

        // Stop just short of the first wall in the way
        int count = Physics2D.CircleCast(position, radius, direction, wallFilter, hits, distance);
        for (int i = 0; i < count; i++)
        {
            if (!hits[i].collider.CompareTag(wallTag)) continue;
            distance = Mathf.Max(0f, hits[i].distance - 0.02f);
            pushUntil = 0f;
            break;
        }

        Vector2 target = position + direction * distance;
        if (rb != null) rb.MovePosition(target);
        else transform.position = new Vector3(target.x, target.y, transform.position.z);
    }
}
