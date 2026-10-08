using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Dash (K / B on a gamepad): a quick burst in the direction the player is moving, or the way they face
// when standing still. The player can't be hurt mid-dash, the matching dash animation plays
// (sideways, up or down) and fading afterimages trail behind.
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDash : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("The Dash action from PlayerInputActions (K, or B / Circle on a gamepad).")]
    public InputActionReference dashAction;

    [Header("Dash")]
    public float dashDistance = 3.75f;
    [Tooltip("Seconds the dash lasts. Matches the dash animations.")]
    public float dashTime = 0.2f;
    [Tooltip("Seconds from the start of one dash until the next one is allowed.")]
    public float cooldown = 0.6f;
    [Tooltip("Hits are ignored while dashing.")]
    public bool invulnerable = true;

    [Header("Afterimages")]
    public float ghostInterval = 0.035f;
    public float ghostLifetime = 0.2f;
    public Color ghostColor = new Color(0.55f, 0.7f, 1f, 0.55f);

    public bool IsDashing { get; private set; }
    public float CooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
    // 1 right after a dash, 0 when the dash is ready
    public float CooldownFraction => cooldown <= 0f ? 0f : Mathf.Clamp01(CooldownRemaining / cooldown);

    // Why the dash can't be used right now, for the HUD. Empty when it is ready
    public string BlockedReason
    {
        get
        {
            if (IsDashing || (attack != null && attack.IsAnimationLocked)) return "Busy";
            if (CooldownRemaining > 0f) return "Cooldown";
            return "";
        }
    }

    private PlayerMovement movement;
    private PlayerAttack attack;
    private Health health;
    private Animator animator;
    private Transform visual;
    private SpriteRenderer visualRenderer;
    private float nextDashTime;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<Health>();
        visual = transform.Find("visual");
        if (visual != null)
        {
            animator = visual.GetComponent<Animator>();
            visualRenderer = visual.GetComponent<SpriteRenderer>();
        }
    }

    void OnEnable()
    {
        if (dashAction != null) dashAction.action.Enable();
    }

    void OnDisable()
    {
        if (dashAction != null) dashAction.action.Disable();
        EndDash();
    }

    void Update()
    {
        // Paused: ignore the dash button
        if (Time.timeScale == 0f) return;

        bool pressed = dashAction != null
            ? dashAction.action.WasPressedThisFrame()
            : Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
        if (pressed) TryDash();
    }

    // Public so tests and tools can dash the same way the button does
    public bool TryDash()
    {
        if (IsDashing || Time.time < nextDashTime) return false;
        if (health != null && health.IsDead) return false;
        // Not in the middle of a swing, or a cast before the fireball is out
        if (attack != null && attack.IsAnimationLocked) return false;

        StartCoroutine(Dash());
        return true;
    }

    IEnumerator Dash()
    {
        IsDashing = true;
        nextDashTime = Time.time + cooldown;

        Vector2 direction = movement.IsMoving ? movement.MoveInput : movement.FacingVector;
        if (direction == Vector2.zero) direction = Vector2.down;
        direction.Normalize();

        if (invulnerable && health != null) health.Dodging = true;
        if (animator != null) animator.SetTrigger("Dash");

        // Reuses the knockback push: steering is ignored for the length of the dash, walls still stop it
        movement.ApplyKnockback(direction * (dashDistance / dashTime), dashTime);

        float elapsed = 0f;
        float ghostTimer = 0f;
        while (elapsed < dashTime)
        {
            ghostTimer -= Time.deltaTime;
            if (ghostTimer <= 0f)
            {
                SpawnGhost();
                ghostTimer = ghostInterval;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        EndDash();
    }

    void EndDash()
    {
        if (health != null) health.Dodging = false;
        IsDashing = false;
    }

    // A fading copy of the current frame left behind the player
    void SpawnGhost()
    {
        if (visualRenderer == null || visualRenderer.sprite == null) return;

        var ghost = new GameObject("Dash Ghost");
        ghost.transform.SetPositionAndRotation(visual.position, visual.rotation);
        ghost.transform.localScale = visual.lossyScale;

        var sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = visualRenderer.sprite;
        sr.flipX = visualRenderer.flipX;
        sr.sortingLayerID = visualRenderer.sortingLayerID;
        sr.sortingOrder = visualRenderer.sortingOrder - 1;
        sr.sharedMaterial = visualRenderer.sharedMaterial;
        sr.color = ghostColor;

        ghost.AddComponent<DashGhost>().lifetime = ghostLifetime;
    }
}
