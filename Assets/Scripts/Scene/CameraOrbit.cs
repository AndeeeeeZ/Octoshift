using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private Transform orbitPivot;
    [SerializeField] private float sensitivity = 0.1f;

    private PlayerInput input;
    private Vector2 lookInput;
    private bool dragging;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        input = new PlayerInput();
    }

    private void Start()
    {
        Vector3 angles = orbitPivot.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    private void OnEnable()
    {
        input.Enable();
        input.Camera.Drag.performed += OnDrag;
        input.Camera.Drag.canceled += OnDrag;
        input.Camera.Look.performed += OnLook;
    }

    private void OnDisable()
    {
        input.Camera.Drag.performed -= OnDrag;
        input.Camera.Drag.canceled -= OnDrag;
        input.Camera.Look.performed -= OnLook;
        input.Disable(); 
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnDrag(InputAction.CallbackContext context)
    {
        dragging = context.ReadValueAsButton();
    }

    private void LateUpdate()
    {
        if (!dragging)
            return;

        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;

        pitch = Mathf.Clamp(pitch, -80f, 80f);

        orbitPivot.rotation = Quaternion.Euler(
            pitch,
            yaw,
            0f);
    }
}