using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The enemy side of Health: the hit flash, burning, waking a slime that gets hit, and what dying means
// (a big slime splits, anything else is destroyed). Also keeps the list of living enemies
// that the wave spawner counts.
[RequireComponent(typeof(Health))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Hit Flash")]
    public Color hitFlashColor = new Color(1f, 0.45f, 0.3f, 1f);
    public float hitFlashTime = 0.12f;

    [Header("Burning")]
    [Tooltip("Color the enemy flickers toward while on fire.")]
    public Color burnColor = new Color(1f, 0.55f, 0.15f, 1f);
    [Tooltip("How fast the burn flicker pulses.")]
    public float burnFlickerSpeed = 12f;

    // Every enemy that is alive right now, babies included
    private static readonly HashSet<EnemyHealth> alive = new HashSet<EnemyHealth>();
    public static int AliveCount { get { Prune(); return alive.Count; } }
    public static IReadOnlyCollection<EnemyHealth> Alive { get { Prune(); return alive; } }
    public static event Action AliveCountChanged;

    // Read by the Test Menu from prefabs, so it goes straight to the Health component
    public int maxHealth => Health.maxHealth;

    public Health Health
    {
        get
        {
            if (health == null) health = GetComponent<Health>();
            return health;
        }
    }

    public bool IsBurning => burnTimeLeft > 0f;

    // Tests replace what death does, since they cannot spawn real slimes
    [NonSerialized] public Action onDeathOverride;

    private Health health;
    private BigSlime bigSlime;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool flashing;

    private float burnTimeLeft;
    private float burnTickTimer;
    private float burnTickInterval;
    private int burnDamagePerTick;

    // Public so EditMode tests can call it. Awake, not OnEnable or Start: a slime made by a merge
    // has to count as alive before the two babies that made it are gone, or a wave would look cleared
    public void Awake()
    {
        bigSlime = GetComponent<BigSlime>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        Health.Damaged -= OnDamaged;
        Health.Died -= OnDied;
        Health.Damaged += OnDamaged;
        Health.Died += OnDied;

        Register(this);
    }

    void OnDestroy()
    {
        Unregister(this);
    }

    void Update()
    {
        if (burnTimeLeft <= 0f || Health.IsDead) return;

        // Burn: damage every tick until the time runs out
        burnTimeLeft -= Time.deltaTime;
        burnTickTimer -= Time.deltaTime;
        if (burnTickTimer <= 0f)
        {
            burnTickTimer += burnTickInterval;
            Health.TakeDamage(burnDamagePerTick);
            if (Health.IsDead) return;
        }

        // The hit flash wins while it shows; otherwise flicker, and go back to normal when the burn ends
        if (spriteRenderer != null && !flashing)
        {
            if (burnTimeLeft > 0f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * burnFlickerSpeed);
                spriteRenderer.color = Color.Lerp(originalColor, burnColor, 0.35f + 0.45f * pulse);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    // Gives this enemy a different amount of health than its prefab has, at full health
    public void SetMaxHealth(int amount)
    {
        Health.SetMaxHealth(amount, true);
    }

    public void TakeDamage(int damage)
    {
        Health.TakeDamage(damage);
    }

    // A hit that also pushes the enemy back (if it survives and has an EnemyKnockback)
    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (!Health.TakeDamage(damage) || Health.IsDead) return;
        EnemyKnockback push = GetComponent<EnemyKnockback>();
        if (push != null) push.Push(knockback);
    }

    // Sets the enemy on fire. Hitting a burning enemy again restarts the burn instead of stacking it.
    public void ApplyBurn(float duration, int damagePerTick, float tickInterval)
    {
        if (Health.IsDead || duration <= 0f) return;

        bool wasBurning = IsBurning;
        burnTimeLeft = duration;
        burnDamagePerTick = damagePerTick;
        burnTickInterval = Mathf.Max(0.05f, tickInterval);
        if (!wasBurning) burnTickTimer = burnTickInterval; // first tick one interval after catching fire
    }

    void OnDamaged()
    {
        // Getting hit makes the slime notice the player, even from behind
        if (bigSlime != null) bigSlime.Alert();

        if (spriteRenderer != null && isActiveAndEnabled)
        {
            StopAllCoroutines();
            StartCoroutine(HitFlash());
        }
    }

    IEnumerator HitFlash()
    {
        flashing = true;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashTime);
        spriteRenderer.color = originalColor;
        flashing = false;
    }

    void OnDied()
    {
        burnTimeLeft = 0f;

        if (onDeathOverride != null)
        {
            onDeathOverride();
        }
        else if (bigSlime != null)
        {
            // Spawns 2 baby slimes and destroys this big slime
            bigSlime.DieAndSplit();
        }
        else
        {
            Destroy(gameObject);
        }

        // Only after the babies exist, so the count never touches 0 during a split
        Unregister(this);
    }

    static void Register(EnemyHealth enemy)
    {
        if (alive.Add(enemy)) AliveCountChanged?.Invoke();
    }

    static void Unregister(EnemyHealth enemy)
    {
        if (alive.Remove(enemy)) AliveCountChanged?.Invoke();
    }

    // OnDestroy only runs for objects whose Awake ran, so an enemy destroyed some other way
    // (inactive, or in the editor) would stay counted forever; drop anything already gone
    static void Prune()
    {
        if (alive.RemoveWhere(enemy => enemy == null) > 0) AliveCountChanged?.Invoke();
    }

    // Tests start from an empty list. Entering Play mode without a domain reload would
    // also keep the old list, so it is cleared then too
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetRegistry()
    {
        alive.Clear();
    }
}
