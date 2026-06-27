using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;

    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Components")]
    [SerializeField] private GroundDetection groundDetection;
    [SerializeField] private SoundEffectPlayer soundEffectPlayer;
    [SerializeField] private MessageController messageController;

    private PlayerInput input;
    private StickToWall stickToWall;
    private CustomGravity customGravity;
    private Animator animator;
    private SpriteRenderer sr;
    private Rigidbody rb;
    private float moveDirection = 0f;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private void Awake()
    {
        input = new PlayerInput();
        rb = GetComponent<Rigidbody>();
        stickToWall = GetComponent<StickToWall>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        customGravity = GetComponent<CustomGravity>();
    }

    private void Update()
    {
        UpdateJumpTimers();
        TryBufferedJump();

        UpdateAnimations();
        UpdateGravity();
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
        input.Player.Jump.canceled += OnReleaseJump;
        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnStopMoving;
    }

    private void OnDisable()
    {
        input.Player.Jump.performed -= OnJump;
        input.Player.Jump.canceled -= OnReleaseJump;
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

        jumpBufferTimer = jumpBufferTime;
    }

    private void OnReleaseJump(InputAction.CallbackContext context)
    {
        customGravity.Falling();
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

    private void UpdateJumpTimers()
    {
        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);

        if (groundDetection.IsGrounded())
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.deltaTime);
        }
    }

    private void TryBufferedJump()
    {
        if (jumpBufferTimer <= 0f)
        {
            return;
        }

        if (coyoteTimer <= 0f)
        {
            return;
        }

        PerformJump();

        animator.SetTrigger("jump");
        soundEffectPlayer.PlayClip();
        customGravity.Jumping();

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    #endregion

    // Check if the player is attached to a block and if is not moving and grounded
    public bool IsAtStablePosition()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Debug.LogWarning("Player is moving");
            messageController.DisplayMessage("Player can't be moving during rotation!");
            return false;
        }

        if (!stickToWall.IsOnABlock())
        {
            Debug.LogWarning("Player is not on a block");
            messageController.DisplayMessage("Player should be fully on a cube during rotation!");
            return false;
        }

        if (!groundDetection.IsGrounded())
        {
            Debug.LogWarning("Player is not grounded");
            messageController.DisplayMessage("Player should be standing on the floor during rotation!");
            return false;
        }
        return true;
    }

    private void UpdateAnimations()
    {
        float verticalSpeed = Vector3.Dot(rb.linearVelocity, transform.up);

        bool grounded = groundDetection.IsGrounded();

        animator.SetBool("grounded", grounded);

        // Moving animation
        animator.SetBool("isMoving", Mathf.Abs(moveDirection) > 0.01f && grounded);

        animator.SetBool("falling", !grounded && verticalSpeed < 0.01f);
    }

    private void UpdateGravity()
    {
        float verticalSpeed = Vector3.Dot(rb.linearVelocity, transform.up);

        bool grounded = groundDetection.IsGrounded();
        if (!grounded && verticalSpeed < -0.1f)
        {
            customGravity.Falling();
        }
    }

    public Vector3 GetCurrBlockPosition()
    {
        if (stickToWall.IsOnABlock())
        {
            return stickToWall.GetCurrStickTargetPosition();
        }
        return transform.position;
    }

}
