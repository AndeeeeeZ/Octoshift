using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;

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
        PerformJump(); 
    }

    #endregion

    #region movement

    private void MovementUpdate()
    {
        Vector3 velocity = rb.linearVelocity; 
        velocity.x = moveSpeed * moveDirection; 
        rb.linearVelocity = velocity; 
    }

    private void PerformJump()
    {
        rb.AddForce(jumpForce * Vector3.up, ForceMode.Impulse); 
    }



    #endregion

}
