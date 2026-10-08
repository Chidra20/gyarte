using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fire bolt shot by the fire spell. Bursts out of the hand, flies forward looping its flame frames,
// pierces through enemies (hitting each once), then fizzles out at a wall or its max range.
public class FireProjectile : MonoBehaviour
{
    [Header("Frames (drawn pointing right)")]
    [Tooltip("Played once as the fire leaves the hand.")]
    public Sprite[] launchFrames;
    [Tooltip("Looped while flying.")]
    public Sprite[] flyFrames;
    [Tooltip("Played once when the bolt hits something or runs out of range.")]
    public Sprite[] fadeFrames;
    public float framesPerSecond = 18f;

    [Header("Frames for shooting down (optional, drawn pointing down)")]
    [Tooltip("Used instead of the frames above when the fireball goes down. Leave empty to rotate the normal frames.")]
    public Sprite[] downLaunchFrames;
    public Sprite[] downFlyFrames;
    public Sprite[] downFadeFrames;
    [Tooltip("Distance from the top of the down flame to its head.")]
    public float downHeadOffset = 0.9f;

    [Header("Flight")]
    public float speed = 8f;
    public float maxDistance = 7f;
    [Tooltip("Distance from the back of the flame to its head, where hits are checked.")]
    public float headOffset = 0.6f;
    public float hitRadius = 0.3f;
    public string wallTag = "wall";

    [Header("Burn")]
    [Tooltip("How long enemies hit by the fireball keep burning.")]
    public float burnDuration = 3f;
    public int burnDamagePerTick = 1;
    [Tooltip("Seconds between burn damage ticks.")]
    public float burnTickInterval = 1f;

    [Header("Glow")]
    public Light2D glow;
    public float glowIntensity = 1.5f;

    private enum Phase { Launch, Fly, Fade }
    private Phase phase = Phase.Launch;

    private SpriteRenderer spriteRenderer;
    private Vector2 direction;
    private int damage;
    private LayerMask enemyLayers;
    private float knockback;
    private float travelled;
    private float frameTimer;
    private int frameIndex;

    // The frame set and head distance in use for this shot (normal or down-facing)
    private Sprite[] activeLaunch, activeFly, activeFade;
    private float activeHeadOffset;

    private ContactFilter2D wallFilter = new ContactFilter2D { useTriggers = false };
    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[8];

    // Enemies that existed when the fireball was cast. Ones that spawn mid-flight (like the babies
    // a slime splits into) aren't hit, so one fireball can't wipe out a whole slime.
    private readonly HashSet<EnemyHealth> targets = new HashSet<EnemyHealth>();
    private readonly HashSet<EnemyHealth> alreadyHit = new HashSet<EnemyHealth>();

    // knockback: how hard each enemy hit is pushed along the bolt's direction
    public void Launch(Vector2 direction, int damage, LayerMask enemyLayers, int sortingOrder, float knockback = 0f)
    {
        this.direction = direction.normalized;
        this.damage = damage;
        this.enemyLayers = enemyLayers;
        this.knockback = knockback;

        targets.Clear();
        alreadyHit.Clear();
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude))
            targets.Add(enemy);

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.enabled = true;

        bool useDownFrames = this.direction.y < -0.5f && downFlyFrames != null && downFlyFrames.Length > 0;
        if (useDownFrames)
        {
            // The down flames are already drawn pointing down, so no rotation
            activeLaunch = downLaunchFrames ?? new Sprite[0];
            activeFly = downFlyFrames;
            activeFade = downFadeFrames ?? new Sprite[0];
            activeHeadOffset = downHeadOffset;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (glow != null) glow.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        }
        else
        {
            // Aim, and flip vertically when going left so the flame isn't drawn upside down
            activeLaunch = launchFrames;
            activeFly = flyFrames;
            activeFade = fadeFrames;
            activeHeadOffset = headOffset;
            float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(1f, this.direction.x < -0.1f ? -1f : 1f, 1f);
            if (glow != null) glow.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        }

        if (glow != null)
        {
            EnsureGlobalLight();
            glow.enabled = true;
        }
        SetPhase(activeLaunch.Length > 0 ? Phase.Launch : Phase.Fly);
        gameObject.SetActive(true);
    }

    // With no 2D lights, Unity draws every sprite fully bright. The moment the glow (a light) shows up,
    // lighting turns on and everything outside the glow goes dark. A plain white global light keeps
    // the scene looking exactly as before, with the glow added on top.
    static void EnsureGlobalLight()
    {
        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
        {
            if (light.lightType == Light2D.LightType.Global) return;
        }

        var lightObj = new GameObject("Global Light 2D (auto)");
        var global = lightObj.AddComponent<Light2D>();
        global.lightType = Light2D.LightType.Global;
        global.color = Color.white;
        global.intensity = 1f;
    }

    void Update()
    {
        Animate();

        if (phase == Phase.Fade) return;

        float step = speed * Time.deltaTime;
        Vector2 head = (Vector2)transform.position + direction * activeHeadOffset;

        // Stop at walls instead of flying through them
        int count = Physics2D.CircleCast(head, hitRadius * 0.5f, direction, wallFilter, wallHits, step);
        for (int i = 0; i < count; i++)
        {
            if (wallHits[i].collider.CompareTag(wallTag))
            {
                transform.position += (Vector3)(direction * wallHits[i].distance);
                SetPhase(Phase.Fade);
                return;
            }
        }

        transform.position += (Vector3)(direction * step);
        travelled += step;

        // Piercing: damage every enemy the flame passes through, once each, and keep flying
        Collider2D[] enemies = Physics2D.OverlapCircleAll((Vector2)transform.position + direction * activeHeadOffset, hitRadius, enemyLayers);
        foreach (Collider2D enemy in enemies)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null || !targets.Contains(health) || alreadyHit.Contains(health)) continue;

            alreadyHit.Add(health);
            health.TakeDamage(damage, direction * knockback);
            health.ApplyBurn(burnDuration, burnDamagePerTick, burnTickInterval);
        }

        if (travelled >= maxDistance) SetPhase(Phase.Fade);
    }

    void Animate()
    {
        Sprite[] frames = CurrentFrames();
        if (frames.Length == 0)
        {
            if (phase == Phase.Fade) Destroy(gameObject);
            return;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / framesPerSecond)
        {
            frameTimer = 0f;
            frameIndex++;

            if (frameIndex >= frames.Length)
            {
                if (phase == Phase.Launch) { SetPhase(Phase.Fly); return; }
                if (phase == Phase.Fade) { Destroy(gameObject); return; }
                frameIndex = 0; // keep looping while flying
            }
        }

        spriteRenderer.sprite = frames[frameIndex];

        if (glow != null)
        {
            float strength = phase == Phase.Fade ? 1f - (float)frameIndex / frames.Length : 1f;
            glow.intensity = glowIntensity * strength * Random.Range(0.85f, 1.15f);
        }
    }

    void SetPhase(Phase newPhase)
    {
        phase = newPhase;
        frameIndex = 0;
        frameTimer = 0f;

        Sprite[] frames = CurrentFrames();
        if (frames.Length > 0 && spriteRenderer != null) spriteRenderer.sprite = frames[0];
    }

    Sprite[] CurrentFrames()
    {
        switch (phase)
        {
            case Phase.Launch: return activeLaunch ?? launchFrames;
            case Phase.Fly: return activeFly ?? flyFrames;
            default: return activeFade ?? fadeFrames;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector2 dir = Application.isPlaying ? direction : (Vector2)transform.right;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + dir * headOffset, hitRadius);
    }
}
