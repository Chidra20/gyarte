using System.Collections.Generic;
using UnityEngine;

public class BigSlime : MonoBehaviour
{
    public GameObject babySlimePrefab;

    [Header("Vision Settings")]
    [Tooltip("How close the player must be before the slime can spot them.")]
    public float detectionRange = 5f;
    [Tooltip("Once chasing, the slime gives up when the player gets further than this.")]
    public float losePlayerRange = 8f;
    [Range(0, 360)] public float fieldOfViewAngle = 110f;
    [Tooltip("How long the slime keeps following the player after losing sight (e.g. after they duck behind a wall).")]
    public float followTime = 2f;

    [Header("Chase Settings")]
    public float chaseSpeed = 2f;
    [Tooltip("The slime stops moving once it is this close to the player.")]
    public float stopDistance = 0.5f;

    [Header("Patrol Settings")]
    public float patrolSpeed = 1f;
    [Tooltip("How far from its starting spot the slime wanders.")]
    public float patrolRadius = 3f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 2.5f;
    [Tooltip("How often the slime turns to look in a new direction while standing still.")]
    public float lookInterval = 0.8f;

    [Header("Search Settings")]
    [Tooltip("How long the slime looks around where it last saw the player.")]
    public float searchTime = 3f;

    [Header("Navigation")]
    [Tooltip("Size of the pathfinding grid cells. Smaller is more precise but slower.")]
    public float cellSize = 0.5f;
    [Tooltip("How often a new path is calculated while walking around walls.")]
    public float repathInterval = 0.4f;

    private enum State { Patrol, Chase, Search }
    private State state = State.Patrol;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool movedThisStep;

    private Vector2 homePosition;
    private Vector2 patrolTarget;
    private Vector2 lastSeenPosition;
    private Vector2 facing = Vector2.right;
    private float bodyRadius = 0.5f;
    private float waitTimer;
    private float lookTimer;
    private float lostSightTimer;

    private GridPathfinder pathfinder;
    private readonly List<Vector2> path = new List<Vector2>();
    private float repathTimer;

    private ContactFilter2D wallFilter = new ContactFilter2D { useTriggers = false };
    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[8];

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) bodyRadius = col.bounds.extents.x;
        pathfinder = new GridPathfinder(cellSize, bodyRadius * 0.9f);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        homePosition = transform.position;
        Face(Random.value < 0.5f ? Vector2.left : Vector2.right);
        StartWaiting();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        movedThisStep = false;

        switch (state)
        {
            case State.Patrol:
                if (CanSeePlayer(detectionRange, true)) StartChase();
                else Patrol();
                break;

            case State.Chase:
                Chase();
                break;

            case State.Search:
                if (CanSeePlayer(detectionRange, true)) StartChase();
                else Search();
                break;
        }

        UpdateAnimation();
    }

    // Only play the hop animation while moving; hold the first frame when standing still
    void UpdateAnimation()
    {
        if (animator == null) return;

        if (movedThisStep)
        {
            animator.speed = 1f;
        }
        else if (animator.speed != 0f)
        {
            animator.Play(0, 0, 0f);
            animator.Update(0f);
            animator.speed = 0f;
        }
    }

    // ---------- Patrol ----------

    void Patrol()
    {
        if (waitTimer > 0)
        {
            LookAround();
            if (waitTimer <= 0) PickPatrolTarget();
            return;
        }

        if (MoveTowards(patrolTarget, patrolSpeed)) StartWaiting();
    }

    void PickPatrolTarget()
    {
        // Try a few random spots around home and take the first one that isn't inside a wall
        for (int i = 0; i < 10; i++)
        {
            Vector2 candidate = homePosition + Random.insideUnitCircle * patrolRadius;
            if (pathfinder.IsWalkable(candidate))
            {
                patrolTarget = candidate;
                return;
            }
        }

        StartWaiting();
    }

    void StartWaiting()
    {
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
        lookTimer = lookInterval;
    }

    // Stand still and glance in random directions, scouting the area
    void LookAround()
    {
        waitTimer -= Time.fixedDeltaTime;
        lookTimer -= Time.fixedDeltaTime;

        if (lookTimer <= 0)
        {
            Face(Random.insideUnitCircle.normalized);
            lookTimer = lookInterval;
        }
    }

    // ---------- Chase ----------

    void StartChase()
    {
        state = State.Chase;
        lostSightTimer = 0;
        lastSeenPosition = player.position;
        path.Clear();
    }

    void Chase()
    {
        // While chasing the slime is locked on, so only range and walls can break sight
        bool inSight = CanSeePlayer(losePlayerRange, false);
        if (inSight) lostSightTimer = 0;
        else lostSightTimer += Time.fixedDeltaTime;

        // For a short while after losing sight it still knows which way you went,
        // so it follows you around the wall instead of stopping at the corner
        if (Vector2.Distance(rb.position, player.position) <= losePlayerRange)
            lastSeenPosition = player.position;

        if (lostSightTimer >= followTime)
        {
            state = State.Search;
            waitTimer = 0;
            path.Clear();
            return;
        }

        Vector2 toTarget = lastSeenPosition - rb.position;
        if (inSight && toTarget.magnitude <= stopDistance)
        {
            Face(toTarget.normalized);
            return;
        }

        MoveTowards(lastSeenPosition, chaseSpeed);
    }

    // ---------- Search ----------

    void Search()
    {
        // First walk to where the player was last seen, then look around for a while
        if (waitTimer <= 0)
        {
            if (MoveTowards(lastSeenPosition, patrolSpeed))
            {
                waitTimer = searchTime;
                lookTimer = lookInterval;
            }
            return;
        }

        LookAround();
        if (waitTimer <= 0)
        {
            state = State.Patrol;
            PickPatrolTarget();
        }
    }

    // ---------- Helpers ----------

    bool CanSeePlayer(float range, bool useFieldOfView)
    {
        if (player == null) return false;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        if (toPlayer.magnitude > range) return false;

        if (useFieldOfView && Vector2.Angle(facing, toPlayer) > fieldOfViewAngle / 2f) return false;

        return !WallBetween(rb.position, player.position);
    }

    // Walks toward the target, pathing around walls when needed.
    // Returns true when the target is reached or can't be reached at all.
    bool MoveTowards(Vector2 target, float speed)
    {
        if ((target - rb.position).magnitude <= 0.05f) return true;

        Vector2 waypoint = target;

        if (pathfinder.IsClear(rb.position, target))
        {
            path.Clear();
        }
        else
        {
            repathTimer -= Time.fixedDeltaTime;
            if (path.Count == 0 || repathTimer <= 0)
            {
                repathTimer = repathInterval;
                if (!pathfinder.FindPath(rb.position, target, path)) return true;
            }

            while (path.Count > 0 && (path[0] - rb.position).magnitude <= 0.1f) path.RemoveAt(0);
            if (path.Count == 0) return true;

            waypoint = path[0];
        }

        Vector2 toWaypoint = waypoint - rb.position;
        float distance = toWaypoint.magnitude;
        if (distance <= 0.001f) return false;

        Vector2 direction = toWaypoint / distance;
        float step = Mathf.Min(speed * Time.fixedDeltaTime, distance);

        rb.MovePosition(rb.position + direction * step);
        Face(direction);
        movedThisStep = true;
        return false;
    }

    bool WallBetween(Vector2 from, Vector2 to)
    {
        int count = Physics2D.Linecast(from, to, wallFilter, wallHits);
        for (int i = 0; i < count; i++)
        {
            if (wallHits[i].collider.CompareTag("wall")) return true;
        }
        return false;
    }

    void Face(Vector2 direction)
    {
        if (direction == Vector2.zero) return;
        facing = direction;

        // Sprite faces right by default, so flip it when looking left
        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.1f)
            spriteRenderer.flipX = direction.x < 0;
    }

    public void DieAndSplit()
    {
        if (babySlimePrefab != null)
        {
            // 1. Spawn two baby slimes slightly offset to the left and right
            Vector3 spawnOffsetLeft = transform.position + new Vector3(-0.5f, 0, 0);
            Vector3 spawnOffsetRight = transform.position + new Vector3(0.5f, 0, 0);

            GameObject baby1Obj = Instantiate(babySlimePrefab, spawnOffsetLeft, Quaternion.identity);
            GameObject baby2Obj = Instantiate(babySlimePrefab, spawnOffsetRight, Quaternion.identity);

            // 2. Link them together as partners so they know who to merge with
            BabySlime baby1 = baby1Obj.GetComponent<BabySlime>();
            BabySlime baby2 = baby2Obj.GetComponent<BabySlime>();

            if (baby1 != null && baby2 != null)
            {
                baby1.partnerSlime = baby2;
                baby2.partnerSlime = baby1;
            }
        }

        // Destroy the Big Slime object
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, losePlayerRange);

        // Field of view cone
        Gizmos.color = Color.red;
        Vector3 left = Quaternion.Euler(0, 0, fieldOfViewAngle / 2f) * facing;
        Vector3 right = Quaternion.Euler(0, 0, -fieldOfViewAngle / 2f) * facing;
        Gizmos.DrawLine(pos, pos + left * detectionRange);
        Gizmos.DrawLine(pos, pos + right * detectionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)homePosition : pos, patrolRadius);

        // Current path around walls
        Gizmos.color = Color.green;
        Vector3 previous = pos;
        foreach (Vector2 point in path)
        {
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }
}
