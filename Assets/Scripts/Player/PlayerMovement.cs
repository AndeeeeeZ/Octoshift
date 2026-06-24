using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [Header("Components")]
    [SerializeField] private GroundDetection groundDetection;

    private PlayerInput input;
    private Rigidbody rb;
    private float moveDirection = 0f;

    private void Awake()
    {
        input = new PlayerInput();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
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

}
