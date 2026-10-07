using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Attack action: scythe swing (melee). Projectile action: fire spell that shoots a fireball,
// with charges that recharge over time. The buttons and keys are set in PlayerInputActions.
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public int attackDamage = 1;
    [Tooltip("How hard a swing pushes an enemy back (speed of the push, units per second). 0 = no knockback.")]
    public float meleeKnockback = 8f;
    [Tooltip("How hard a fireball pushes an enemy back, in the direction it flies. 0 = no knockback.")]
    public float spellKnockback = 5f;
    public LayerMask enemyLayers;

    [Header("Scythe Swing")]
    [Tooltip("The Attack action from PlayerInputActions (Square / X on a gamepad).")]
    public InputActionReference meleeAction;
    [Tooltip("Center of the melee hitbox. Created automatically if left empty.")]
    public Transform attackPoint;
    public float attackRange = 0.75f;
    [Tooltip("How far in front of the player the hitbox sits.")]
    public float attackPointDistance = 0.95f;
    [Tooltip("Moves the hitbox down a little when facing left/right, where the scythe sweeps lower.")]
    public float attackPointSidewaysHeight = -0.15f;
    [Tooltip("Seconds into the Swing animation when the scythe is in front and enemies get hit.")]
    public float swingHitTime = 0.18f;
    [Tooltip("Length of the Swing animation. You can't swing again or cast until it ends.")]
    public float swingDuration = 0.44f;
    [Tooltip("Seconds from the start of one swing until the next one is allowed. Keep it at least as long as swingDuration.")]
    public float meleeCooldown = 0.6f;

    [Header("Melee Hitbox Display")]
    public bool showHitbox = false;
    public Color hitboxColor = new Color(1f, 0.1f, 0.1f, 0.9f);
    [Tooltip("Color the hitbox flashes when you attack.")]
    public Color hitboxFlashColor = new Color(1f, 0.9f, 0.2f, 1f);
    public float lineWidth = 0.08f;
    [Range(0f, 1f)] public float fillAlpha = 0.25f;
    [Range(0f, 1f)] public float flashFillAlpha = 0.6f;
    public float flashTime = 0.15f;

    [Header("Fire Spell")]
    [Tooltip("The Projectile action from PlayerInputActions (Triangle / Y on a gamepad).")]
    public InputActionReference castAction;
    [Tooltip("How many fire spells can be stored up and cast back to back.")]
    public int maxSpellCharges = 2;
    [Tooltip("Seconds to get one charge back. Charges come back one at a time.")]
    public float spellRechargeTime = 5f;
    public FireProjectile projectilePrefab;
    [Tooltip("Must match the frame rate of the FireSpell animation clip.")]
    public float spellFramesPerSecond = 18f;
    [Tooltip("Total frames in the cast animation.")]
    public int spellTotalFrames = 11;
    [Tooltip("Cast frame where the fire leaves the hand.")]
    public int fireStartFrame = 3;
    [Tooltip("Makes the player stand still while casting.")]
    public bool lockMovementWhileCasting = false;

    [Header("Casting Hand")]
    [Tooltip("How far from the player's center the fire starts (roughly the hand).")]
    public float handDistance = 0.4f;
    [Tooltip("Vertical offset of the hand when casting sideways.")]
    public float handHeight = -0.07f;
    [Tooltip("Sideways shift toward the casting hand when shooting up or down.")]
    public float sideHandOffset = 0.25f;

    public bool IsCasting { get; private set; }
    public bool IsSwinging { get; private set; }
    public bool IsAttacking => IsCasting || IsSwinging;
    public int SpellCharges { get; private set; }
    // 0 to 1 progress toward the next charge (stays 0 while full)
    public float RechargeProgress => SpellCharges >= maxSpellCharges ? 0f : rechargeTimer / spellRechargeTime;
    public float SecondsToNextCharge => SpellCharges >= maxSpellCharges ? 0f : Mathf.Max(0f, spellRechargeTime - rechargeTimer);

    public float MeleeCooldownRemaining => Mathf.Max(0f, nextSwingTime - Time.time);
    // 1 right after a swing, 0 when the swing is ready
    public float MeleeCooldownFraction => meleeCooldown <= 0f ? 0f : Mathf.Clamp01(MeleeCooldownRemaining / meleeCooldown);

    // Why an attack can't be used right now, for the HUD. Empty when it is ready
    public string MeleeBlockedReason => MeleeReason(IsCasting, MeleeCooldownRemaining);
    public string SpellBlockedReason => SpellReason(IsSwinging, IsCasting, SpellCharges);

    // "Busy" means the other attack (or this cast) is still playing
    public static string MeleeReason(bool casting, float cooldownRemaining)
    {
        if (casting) return "Busy";
        if (cooldownRemaining > 0f) return "Cooldown";
        return "";
    }

    public static string SpellReason(bool swinging, bool casting, int charges)
    {
        if (swinging || casting) return "Busy";
        if (charges <= 0) return "No charges";
        return "";
    }

    private PlayerMovement movement;
    private Transform visual;
    private Animator animator;
    private SpriteRenderer playerSprite;
    private float rechargeTimer;
    private float nextSwingTime;

    private const int CircleSegments = 40;
    private LineRenderer hitboxLine;
    private SpriteRenderer hitboxFill;
    private float drawnRadius = -1f;
    private float flashTimer;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        visual = transform.Find("visual");
        if (visual != null)
        {
            animator = visual.GetComponent<Animator>();
            playerSprite = visual.GetComponent<SpriteRenderer>();
        }
        if (enemyLayers.value == 0) enemyLayers = LayerMask.GetMask("Enemy");

        if (attackPoint == null)
        {
            attackPoint = new GameObject("Attack").transform;
            attackPoint.SetParent(transform, false);
        }

        SpellCharges = maxSpellCharges;
        CreateHitboxDisplay();
    }

    void OnEnable()
    {
        if (meleeAction != null) meleeAction.action.Enable();
        if (castAction != null) castAction.action.Enable();
    }

    void OnDisable()
    {
        if (meleeAction != null) meleeAction.action.Disable();
        if (castAction != null) castAction.action.Disable();
    }

    void Update()
    {
        // Paused: ignore attack buttons
        if (Time.timeScale == 0f) return;

        RechargeSpell();
        UpdateAttackPoint();

        if (meleeAction != null && meleeAction.action.WasPressedThisFrame()) TryMelee();
        if (castAction != null && castAction.action.WasPressedThisFrame()) TryCast();
    }

    // Public so tests and tools can trigger the attacks the same way the buttons do
    public bool TryMelee()
    {
        if (IsSwinging || MeleeBlockedReason != "") return false;
        nextSwingTime = Time.time + Mathf.Max(meleeCooldown, swingDuration);
        StartCoroutine(Swing());
        return true;
    }

    public bool TryCast()
    {
        if (SpellBlockedReason != "") return false;
        StartCoroutine(CastFireSpell());
        return true;
    }

    Vector2 Facing()
    {
        return movement != null ? movement.FacingVector : Vector2.right;
    }

    // ---------- Scythe Swing ----------

    // Plays the Swing animation and hits when the scythe comes around in front
    IEnumerator Swing()
    {
        IsSwinging = true;
        if (animator != null) animator.SetTrigger("Swing");

        yield return new WaitForSeconds(swingHitTime);
        MeleeAttack();

        yield return new WaitForSeconds(Mathf.Max(0f, swingDuration - swingHitTime));
        IsSwinging = false;
    }

    void MeleeAttack()
    {
        flashTimer = flashTime;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null) continue;
            Vector2 away = EnemyKnockback.Direction(transform.position, enemy.transform.position, Facing());
            health.TakeDamage(attackDamage, away * meleeKnockback);
        }
    }

    // Keep the hitbox in front of the player, on the side they're facing
    void UpdateAttackPoint()
    {
        Vector2 facing = Facing();
        Vector3 offset = facing * attackPointDistance;
        if (Mathf.Abs(facing.x) > 0.5f) offset.y += attackPointSidewaysHeight;
        attackPoint.position = transform.position + offset;
    }

    void CreateHitboxDisplay()
    {
        // Circle drawn around the attack point that matches the OverlapCircle used for melee hits
        GameObject hitboxObj = new GameObject("Hitbox Display");
        hitboxObj.transform.SetParent(attackPoint, false);

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        hitboxLine = hitboxObj.AddComponent<LineRenderer>();
        hitboxLine.useWorldSpace = false;
        hitboxLine.loop = true;
        hitboxLine.positionCount = CircleSegments;
        hitboxLine.sortingOrder = 101;
        hitboxLine.material = new Material(shader);

        // Semi-transparent filled disc inside the outline
        GameObject fillObj = new GameObject("Hitbox Fill");
        fillObj.transform.SetParent(hitboxObj.transform, false);
        hitboxFill = fillObj.AddComponent<SpriteRenderer>();
        hitboxFill.sprite = CreateCircleSprite(64);
        hitboxFill.sortingOrder = 100;
        hitboxFill.material = new Material(shader);
    }

    void LateUpdate()
    {
        if (hitboxLine == null) return;

        hitboxLine.enabled = showHitbox;
        hitboxFill.enabled = showHitbox;
        if (!showHitbox) return;

        // Rebuild the circle if the range was changed in the Inspector
        if (!Mathf.Approximately(drawnRadius, attackRange))
        {
            drawnRadius = attackRange;
            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegments;
                hitboxLine.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * attackRange);
            }

            // The circle sprite is 1 unit across, so scale it to the hitbox diameter
            hitboxFill.transform.localScale = Vector3.one * attackRange * 2f;
        }

        if (flashTimer > 0) flashTimer -= Time.deltaTime;
        bool flashing = flashTimer > 0;

        Color color = flashing ? hitboxFlashColor : hitboxColor;
        hitboxLine.widthMultiplier = flashing ? lineWidth * 1.5f : lineWidth;
        hitboxLine.startColor = color;
        hitboxLine.endColor = color;

        color.a = flashing ? flashFillAlpha : fillAlpha;
        hitboxFill.color = color;
    }

    Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ---------- Fire Spell ----------

    // Charges come back one at a time, each taking spellRechargeTime seconds
    void RechargeSpell()
    {
        if (SpellCharges >= maxSpellCharges)
        {
            rechargeTimer = 0f;
            return;
        }

        rechargeTimer += Time.deltaTime;
        if (rechargeTimer >= spellRechargeTime)
        {
            rechargeTimer -= spellRechargeTime;
            SpellCharges++;
        }
    }

    // Used by abilities. fill: also top the current charges up to the new maximum
    public void SetMaxSpellCharges(int max, bool fill)
    {
        maxSpellCharges = Mathf.Max(1, max);
        SpellCharges = fill ? maxSpellCharges : Mathf.Min(SpellCharges, maxSpellCharges);
    }

    // Adds charges right away, never above the maximum
    public void AddSpellCharges(int amount)
    {
        SpellCharges = Mathf.Clamp(SpellCharges + amount, 0, maxSpellCharges);
    }

    // Fills every charge back up at once
    public void RefillSpellCharges()
    {
        SpellCharges = maxSpellCharges;
        rechargeTimer = 0f;
    }

    // Plays the cast animation and shoots the fireball on the frame it leaves the hand
    IEnumerator CastFireSpell()
    {
        IsCasting = true;
        SpellCharges--;

        Vector2 direction = Facing();
        float frameTime = 1f / spellFramesPerSecond;

        if (animator != null) animator.SetTrigger("Attack");

        yield return new WaitForSeconds(fireStartFrame * frameTime);
        ShootProjectile(direction);

        yield return new WaitForSeconds((spellTotalFrames - fireStartFrame) * frameTime);
        IsCasting = false;
    }

    void ShootProjectile(Vector2 direction)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("No fire projectile prefab assigned on PlayerAttack!");
            return;
        }

        // The visual is mirrored (scale x = -1) when facing left
        float side = visual != null && visual.localScale.x < 0 ? -1f : 1f;

        Vector2 hand = direction * handDistance;
        if (Mathf.Abs(direction.x) > 0.5f)
            hand.y += handHeight;
        else
            hand.x += side * sideHandOffset; // casting up/down: start from the hand, not the head

        // Shooting up starts behind the player, everything else in front
        int playerOrder = playerSprite != null ? playerSprite.sortingOrder : 0;
        int order = direction.y > 0.5f ? playerOrder - 1 : playerOrder + 1;

        FireProjectile projectile = Instantiate(projectilePrefab, transform.position + (Vector3)hand, Quaternion.identity);
        projectile.Launch(direction, attackDamage, enemyLayers, order, spellKnockback);
    }
}
