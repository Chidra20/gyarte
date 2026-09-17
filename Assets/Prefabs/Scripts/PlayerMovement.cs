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
        animator = GetComponent<Animator>();

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

        if (movement != Vector2.zero)
        {
            if (Mathf.Abs(movement.y) > Mathf.Abs(movement.x))
            {
                rb.transform.rotation = Quaternion.Euler(0f, 0f, movement.y > 0f ? 90f : -90f);
            }
            else
            {
                rb.transform.rotation = Quaternion.Euler(0f, movement.x < 0f ? -180f : 0f, 0f);
            }
        }
    }
}