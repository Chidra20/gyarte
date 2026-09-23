using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Input")]
    public InputActionReference moveAction;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        Transform visual = transform.Find("visual");
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
        movement = moveAction.action.ReadValue<Vector2>();
        movement = movement.normalized;

        bool primarilyVertical = Mathf.Abs(movement.y) > Mathf.Abs(movement.x);
        animator.SetBool("IsRunning", movement != Vector2.zero && !primarilyVertical);
        animator.SetBool("RunningUp", primarilyVertical && movement.y > 0f);
        animator.SetBool("RunningDown", primarilyVertical && movement.y < 0f);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }
}