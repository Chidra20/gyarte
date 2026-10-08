using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private enum FacingDirection
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3
    }

    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("Multiplies moveSpeed. Abilities raise it; 1 is normal speed.")]
    public float speedMultiplier = 1f;

    [Header("Input")]
    public InputActionReference moveAction;

    private Rigidbody2D rb;
    private Animator animator;
    [SerializeField] private Transform visual;
    [SerializeField] private Transform facingObject;
    [SerializeField] private float facingObjectDistance = 1f;
    private Vector2 movement;
    private FacingDirection facingDirection = FacingDirection.Down;
    private PlayerAttack attack;
    private Vector2 knockbackVelocity;
    private float knockbackUntil;

    public bool IsMoving => movement != Vector2.zero;
    // The stick / WASD direction this frame (normalized, zero when not moving)
    public Vector2 MoveInput => movement;

    private PlayerDash dash;

    // Which way the player faces, as a direction (used to aim attacks)
    public Vector2 FacingVector => facingDirection switch
    {
        FacingDirection.Up => Vector2.up,
        FacingDirection.Left => Vector2.left,
        FacingDirection.Right => Vector2.right,
        _ => Vector2.down
    };

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        attack = GetComponent<PlayerAttack>();
        dash = GetComponent<PlayerDash>();

        if (visual == null)
        {
            visual = transform.Find("visual");
        }
        if (visual != null)
        {
            animator = visual.GetComponent<Animator>();
        }

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void OnEnable()
    {
        moveAction.action.Enable();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
    }

    void Update()
    {
        // Paused: keep the current pose instead of turning on the spot
        if (Time.timeScale == 0f) return;

        movement = moveAction.action.ReadValue<Vector2>();
        movement = movement.normalized;

        // While the attack pose is locked (the whole swing, or a cast until the fireball is out):
        // keep facing the same way and let the attack animation play
        // A dash locks the same way, so the dash animation plays instead of the run cycle
        bool attacking = (attack != null && attack.IsAnimationLocked) || (dash != null && dash.IsDashing);
        if (attack != null && attack.IsCasting && attack.lockMovementWhileCasting)
            movement = Vector2.zero;

        bool primarilyVertical = Mathf.Abs(movement.y) > Mathf.Abs(movement.x);
        if (movement != Vector2.zero && !attacking)
        {
            if (primarilyVertical)
            {
                facingDirection = movement.y > 0f ? FacingDirection.Up : FacingDirection.Down;
            }
            else
            {
                facingDirection = movement.x > 0f ? FacingDirection.Right : FacingDirection.Left;
            }
        }

        if (visual != null)
        {
            Vector3 visualScale = visual.localScale;
            float horizontalDirection = facingDirection == FacingDirection.Left ? -1f : 1f;
            visual.localScale = new Vector3(
                Mathf.Abs(visualScale.x) * horizontalDirection,
                visualScale.y,
                visualScale.z);
        }

        int animationDirection = facingDirection == FacingDirection.Left
            ? (int)FacingDirection.Right
            : (int)facingDirection;
        animator.SetInteger("Direction", animationDirection);
        animator.SetBool("IsRunning", !attacking && movement != Vector2.zero && !primarilyVertical);
        animator.SetBool("RunningUp", !attacking && primarilyVertical && movement.y > 0f);
        animator.SetBool("RunningDown", !attacking && primarilyVertical && movement.y < 0f);

        if (facingObject != null)
        {
            Vector3 facingOffset = facingDirection switch
            {
                FacingDirection.Up => Vector3.up,
                FacingDirection.Left => Vector3.left,
                FacingDirection.Right => Vector3.right,
                _ => Vector3.down
            };

            facingObject.position = transform.position + facingOffset * facingObjectDistance;
        }
    }

    // Pushes the player for a moment, e.g. when an enemy hits them. Steering is ignored meanwhile,
    // otherwise the next physics step would overwrite the push with the stick input
    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        knockbackVelocity = velocity;
        knockbackUntil = Time.time + duration;
    }

    void FixedUpdate()
    {
        if (Time.time < knockbackUntil)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        rb.linearVelocity = movement * moveSpeed * speedMultiplier;
    }
}