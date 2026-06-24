using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private Transform orbitPivot;
    [SerializeField] private Transform player;
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float maxPitch = 80f;

    private PlayerInput input;
    private Vector2 lookInput;
    private bool dragging;

    private Quaternion yawRotation;
    private float pitch;

    private void Awake()
    {
        input = new PlayerInput();
    }

    private void Start()
    {
        yawRotation = Quaternion.LookRotation(
            player.forward,
            player.up);

        orbitPivot.rotation = yawRotation;

        pitch = 0f;
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

        yawRotation = Quaternion.LookRotation(
            player.forward,
            player.up);

        pitch = 0f;

        orbitPivot.rotation = yawRotation;
    }

    private void LateUpdate()
    {
        if (!dragging)
            return;

        yawRotation =
            Quaternion.AngleAxis(
                lookInput.x * sensitivity,
                player.up) * yawRotation;

        pitch -= lookInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        Vector3 right =
            yawRotation * Vector3.right;

        Quaternion pitchRotation =
            Quaternion.AngleAxis(
                pitch,
                right);

        orbitPivot.rotation =
            pitchRotation * yawRotation;
    }
}