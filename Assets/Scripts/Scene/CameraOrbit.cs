using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private Transform orbitPivot;
    [SerializeField] private Transform player;
    [SerializeField] private float sensitivity = 0.1f;

    private PlayerInput input;
    private Vector2 lookInput;
    private bool dragging;

    private void Awake()
    {
        input = new PlayerInput();
    }

    private void OnEnable()
    {
        input.Enable();

        input.Camera.Drag.performed += OnDrag;
        input.Camera.Drag.canceled += OnDrag;

        input.Camera.Look.performed += OnLook;
        input.Camera.Look.canceled += OnLook;

        input.Camera.Focus.performed += OnFocus;
    }

    private void OnDisable()
    {
        input.Camera.Drag.performed -= OnDrag;
        input.Camera.Drag.canceled -= OnDrag;

        input.Camera.Look.performed -= OnLook;
        input.Camera.Look.canceled -= OnLook;

        input.Camera.Focus.performed -= OnFocus;

        input.Disable();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnDrag(InputAction.CallbackContext context)
    {
        dragging = context.ReadValueAsButton();
    }

    private void OnFocus(InputAction.CallbackContext context)
    {
        orbitPivot.position = player.position;

        orbitPivot.rotation = Quaternion.LookRotation(
            player.forward,
            player.up);
    }

    private void LateUpdate()
    {
        if (!dragging)
            return;

        Vector3 up = orbitPivot.up;
        Vector3 right = orbitPivot.right;

        Quaternion yaw = Quaternion.AngleAxis(
            lookInput.x * sensitivity,
            up);

        Quaternion pitch = Quaternion.AngleAxis(
            -lookInput.y * sensitivity,
            right);

        orbitPivot.rotation =
            yaw * pitch * orbitPivot.rotation;
    }
}