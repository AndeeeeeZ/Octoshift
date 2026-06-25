using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private Transform orbitPivot;
    [SerializeField] private Transform player;
    [SerializeField] private float sensitivity, panSensitivity;

    private PlayerInput input;

    private Vector2 lookInput;
    private bool rotating;

    private bool panning;
    private Plane panPlane;
    private Vector3 lastPanPoint;

    private Camera cam;

    private void Awake()
    {
        input = new PlayerInput();
        cam = Camera.main;
    }

    private void Start()
    {
        Focus(); 
    }

    private void OnEnable()
    {
        input.Enable();

        input.Camera.Drag.performed += OnDrag;
        input.Camera.Drag.canceled += OnDrag;

        input.Camera.Look.performed += OnLook;
        input.Camera.Look.canceled += OnLook;

        input.Camera.Focus.performed += OnFocus;

        input.Camera.Pan.performed += OnPan;
        input.Camera.Pan.canceled += OnPan;
    }

    private void OnDisable()
    {
        input.Camera.Drag.performed -= OnDrag;
        input.Camera.Drag.canceled -= OnDrag;

        input.Camera.Look.performed -= OnLook;
        input.Camera.Look.canceled -= OnLook;

        input.Camera.Focus.performed -= OnFocus;

        input.Camera.Pan.performed -= OnPan;
        input.Camera.Pan.canceled -= OnPan;

        input.Disable();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnDrag(InputAction.CallbackContext context)
    {
        rotating = context.ReadValueAsButton();
    }

    private void OnPan(InputAction.CallbackContext context)
    {
        panning = context.ReadValueAsButton();

        if (!panning)
            return;

        // Plane parallel to the screen through the pivot
        panPlane = new Plane(transform.forward, orbitPivot.position);

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (panPlane.Raycast(ray, out float enter))
        {
            lastPanPoint = ray.GetPoint(enter);
        }
    }

    private void OnFocus(InputAction.CallbackContext context)
    {
        Focus(); 
    }

    private void Focus()
    {
        orbitPivot.position = player.position;

        orbitPivot.rotation = Quaternion.LookRotation(
            player.forward,
            player.up);
    }

    private void LateUpdate()
    {
        HandlePan();
        HandleRotation();
    }

    private void HandleRotation()
    {
        if (!rotating)
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

    private void HandlePan()
    {
        if (!panning)
            return;

        Ray ray = cam.ScreenPointToRay(
            Mouse.current.position.ReadValue());

        if (!panPlane.Raycast(ray, out float enter))
            return;

        Vector3 currentPoint = ray.GetPoint(enter);

        Vector3 delta = lastPanPoint - currentPoint;

        orbitPivot.position += delta * panSensitivity;

        ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (panPlane.Raycast(ray, out enter))
        {
            lastPanPoint = ray.GetPoint(enter);
        }
    }
}