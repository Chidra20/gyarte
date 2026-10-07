using UnityEngine;

public class BabySlime : MonoBehaviour
{
    public int maxHealth = 1;
    private int currentHealth;

    [Header("Fusion Settings")]
    public BabySlime partnerSlime;
    public GameObject bigSlimePrefab;
    public float moveSpeed = 1.5f;

    [Header("Fusion Delay")]
    [Tooltip("Time in seconds before the baby slimes can merge back together.")]
    public float fuseDelay = 0f; // Adjust this in the Inspector
    [Tooltip("How close the two babies need to be to merge.")]
    public float mergeDistance = 0.2f;
    private float timer;
    private bool hasMerged;

    void Start()
    {
        currentHealth = maxHealth;
        timer = fuseDelay; // Start countdown
    }

    private EnemyKnockback knockback;

    void Awake()
    {
        knockback = GetComponent<EnemyKnockback>();
    }

    void Update()
    {
        // Countdown fuse timer
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }

        // Being pushed back by a hit: let EnemyKnockback move it
        if (knockback != null && knockback.IsKnockedBack) return;

        // Always keep moving toward partner
        if (partnerSlime != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                partnerSlime.transform.position,
                moveSpeed * Time.deltaTime
            );

            // Both babies are kinematic, and Unity doesn't report trigger contacts between two
            // kinematic bodies, so check for overlap directly instead of waiting for OnTrigger
            if (timer <= 0 && IsTouchingPartner())
            {
                MergeBackIntoBigSlime();
            }
        }
    }

    // True as soon as the two babies' bodies overlap
    bool IsTouchingPartner()
    {
        Collider2D mine = GetComponent<Collider2D>();
        Collider2D theirs = partnerSlime.GetComponent<Collider2D>();
        if (mine != null && theirs != null && mine.Distance(theirs).isOverlapped) return true;

        return Vector2.Distance(transform.position, partnerSlime.transform.position) <= mergeDistance;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryMerge(other);
    }

    // Handles trigger stays in case they are already touching when the timer hits 0
    void OnTriggerStay2D(Collider2D other)
    {
        TryMerge(other);
    }

    void TryMerge(Collider2D other)
    {
        // Block fusion while timer is running
        if (timer > 0) return;

        BabySlime otherBaby = other.GetComponent<BabySlime>();

        if (otherBaby != null && otherBaby == partnerSlime)
        {
            MergeBackIntoBigSlime();
        }
    }

    void MergeBackIntoBigSlime()
    {
        // Both babies get the trigger callback in the same physics step,
        // so only let the first one do the merge or two big slimes get spawned
        if (hasMerged || partnerSlime.hasMerged) return;
        hasMerged = true;
        partnerSlime.hasMerged = true;

        Vector3 spawnPos = (transform.position + partnerSlime.transform.position) / 2f;

        if (bigSlimePrefab != null)
        {
            Instantiate(bigSlimePrefab, spawnPos, Quaternion.identity);
        }

        Destroy(partnerSlime.gameObject);
        Destroy(gameObject);
    }
}