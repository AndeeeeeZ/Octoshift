using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orbitPivot;
    [SerializeField] private Transform player;

    [Header("Controls")]
    [SerializeField] private float sensitivity = 0.2f;
    [SerializeField] private float panSensitivity = 1f;

    [Header("Focus")]
    [SerializeField] private float focusMoveSpeed = 8f;
    [SerializeField] private float focusRotateSpeed = 8f;

    private PlayerInput input;

    private Vector2 lookInput;
    private bool rotating;

    private bool panning;
    private Plane panPlane;
    private Vector3 lastPanPoint;

    private Camera cam;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool focusing;

    private PlayerMovement playerMovement; 

    private void Awake()
    {
        input = new PlayerInput();
        cam = Camera.main;
    }

    private void Start()
    {
        playerMovement = player.GetComponent<PlayerMovement>(); 

        targetPosition = orbitPivot.position;
        targetRotation = orbitPivot.rotation;

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

        // Cancel focus if the player manually rotates the camera
        if (rotating)
            focusing = false;
    }

    private void OnPan(InputAction.CallbackContext context)
    {
        panning = context.ReadValueAsButton();

        if (!panning)
            return;

        // Cancel focus if the player manually pans the camera
        focusing = false;

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
        targetPosition = playerMovement.GetCurrBlockPosition();

        targetRotation = Quaternion.LookRotation(
            player.forward,
            player.up);

        focusing = true;
    }

    private void LateUpdate()
    {
        HandlePan();
        HandleRotation();
        HandleFocus();
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

    private void HandleFocus()
    {
        if (!focusing)
            return;

        // Exponential smoothing for consistent movement
        float moveT = 1f - Mathf.Exp(-focusMoveSpeed * Time.deltaTime);
        float rotateT = 1f - Mathf.Exp(-focusRotateSpeed * Time.deltaTime);

        orbitPivot.position = Vector3.Lerp(
            orbitPivot.position,
            targetPosition,
            moveT);

        orbitPivot.rotation = Quaternion.Slerp(
            orbitPivot.rotation,
            targetRotation,
            rotateT);

        if (Vector3.Distance(orbitPivot.position, targetPosition) < 0.01f &&
            Quaternion.Angle(orbitPivot.rotation, targetRotation) < 0.2f)
        {
            orbitPivot.position = targetPosition;
            orbitPivot.rotation = targetRotation;
            focusing = false;
        }
    }
}