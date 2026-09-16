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
        animator.SetBool("IsRunning", movement != Vector2.zero);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;

        if (movement.x != 0f)
        {
            rb.transform.rotation = Quaternion.Euler(0f, movement.x < 0f ? -180f : 0f, 0f);
        }
    }
}