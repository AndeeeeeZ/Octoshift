using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [Header("Components")]
    [SerializeField] private GroundDetection groundDetection;
    [SerializeField] private SoundEffectPlayer soundEffectPlayer; 

    private PlayerInput input;
    private StickToWall stickToWall;
    private Animator animator;
    private SpriteRenderer sr;
    private Rigidbody rb;
    private float moveDirection = 0f;

    private void Awake()
    {
        input = new PlayerInput();
        rb = GetComponent<Rigidbody>();
        stickToWall = GetComponent<StickToWall>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        MovementUpdate();
    }

    #region input
    private void OnEnable()
    {
        input.Enable();
        input.Player.Jump.performed += OnJump;
        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnStopMoving;
    }

    private void OnDisable()
    {
        input.Player.Jump.performed -= OnJump;
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnStopMoving;
        input.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveDirection = context.ReadValue<float>();
        sr.flipX = moveDirection < 0f;
    }

    private void OnStopMoving(InputAction.CallbackContext context)
    {
        moveDirection = 0f;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (groundDetection == null)
        {
            Debug.LogError("Missing ground detection");
            return;
        }
        if (!groundDetection.IsGrounded())
        {
            Debug.LogWarning("Attempted jump but not grounded");
            return;
        }
        PerformJump();
        animator.SetTrigger("jump"); 
        soundEffectPlayer.PlayClip(); 
    }

    #endregion

    #region movement

    private void MovementUpdate()
    {
        Vector3 velocity = rb.linearVelocity;

        Vector3 verticalVelocity =
            Vector3.Project(
                velocity,
                transform.up);

        Vector3 horizontalVelocity =
            transform.right *
            (moveSpeed * moveDirection);

        rb.linearVelocity =
            horizontalVelocity +
            verticalVelocity;
    }

    private void PerformJump()
    {
        Vector3 velocity = rb.linearVelocity;

        velocity -= Vector3.Project(velocity, transform.up);

        rb.linearVelocity = velocity;

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    #endregion

    // Check if the player is attached to a block and if is not moving and grounded
    public bool IsAtStablePosition()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Debug.LogWarning("Player is moving");
            return false;
        }

        if (!stickToWall.IsOnABlock())
        {
            Debug.LogWarning("Player is not on a block");
            return false;
        }

        if (!groundDetection.IsGrounded())
        {
            Debug.LogWarning("Player is not grounded");
            return false;
        }
        return true;
    }

    private void UpdateAnimations()
    {
        float verticalSpeed =
            Vector3.Dot(rb.linearVelocity, transform.up);

        bool grounded = groundDetection.IsGrounded();

        animator.SetBool("grounded", grounded);

        // Moving animation
        animator.SetBool("isMoving", Mathf.Abs(moveDirection) > 0.01f && grounded);

        animator.SetBool("falling", !grounded && verticalSpeed < -0.1f);
    }

}
