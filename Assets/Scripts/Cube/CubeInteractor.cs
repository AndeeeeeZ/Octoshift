using UnityEngine;
using UnityEngine.InputSystem;

public class CubeInteractor : MonoBehaviour
{
    [SerializeField] private Camera orbitCamera;
    [SerializeField] private LayerMask cubeLayer = ~0;

    private PlayerInput input;
    private Vector2 mousePosition;
    private Vector2 mouseDelta;

    private CubeRotator activeRotator;
    private bool dragging;

    private void Awake()
    {
        input = new PlayerInput();
    }

    private void OnEnable()
    {
        input.Enable();

        input.Cube.Look.performed += OnLook;
        input.Cube.Look.canceled += OnLook;

        input.Cube.Drag.performed += OnDrag;
        input.Cube.Drag.canceled += OnDrag;
    }

    private void OnDisable()
    {
        input.Cube.Look.performed -= OnLook;
        input.Cube.Look.canceled -= OnLook;

        input.Cube.Drag.performed -= OnDrag;
        input.Cube.Drag.canceled -= OnDrag;

        input.Disable();
    }

    private void Update()
    {
        if (dragging && activeRotator != null)
        {
            activeRotator.UpdateDrag(mouseDelta);
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        mouseDelta = context.ReadValue<Vector2>();
        mousePosition = Mouse.current.position.ReadValue();
    }

    private void OnDrag(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            BeginDrag();
        }
        else if (context.canceled)
        {
            dragging = false;

            if (activeRotator != null)
            {
                activeRotator.EndDrag();
                activeRotator = null;
            }
        }
    }

    private void BeginDrag()
    {
        if (orbitCamera == null)
            return;

        Ray ray = orbitCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, cubeLayer))
            return;

        CubeRotator rotator = hit.collider.GetComponentInParent<CubeRotator>();
        if (rotator == null)
        {
            Debug.LogWarning("Missing cubeRotator component"); 
            return;
        }

        activeRotator = rotator;
        dragging = true;
        activeRotator.BeginDrag(hit.normal, orbitCamera.transform);
    }
}