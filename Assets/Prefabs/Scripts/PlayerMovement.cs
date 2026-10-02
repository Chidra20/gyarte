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

    [Header("Input")]
    public InputActionReference moveAction;

    private Rigidbody2D rb;
    private Animator animator;
    [SerializeField] private Transform visual;
    [SerializeField] private Transform facingObject;
    [SerializeField] private float facingObjectDistance = 1f;
    private Vector2 movement;
    private FacingDirection facingDirection = FacingDirection.Down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

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

        bool primarilyVertical = Mathf.Abs(movement.y) > Mathf.Abs(movement.x);
        if (movement != Vector2.zero)
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
        animator.SetBool("IsRunning", movement != Vector2.zero && !primarilyVertical);
        animator.SetBool("RunningUp", primarilyVertical && movement.y > 0f);
        animator.SetBool("RunningDown", primarilyVertical && movement.y < 0f);

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

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }
}