using UnityEngine;
using UnityEngine.InputSystem;

public class CubeInteractor : MonoBehaviour
{
    [SerializeField] private PuzzleDragRotator activeRotator;
    [SerializeField] private Camera orbitCamera;
    [SerializeField] private LayerMask cubeLayer = ~0;

    private PlayerInput input;
    private bool dragging;

    private void Awake()
    {
        input = new PlayerInput();
    }

    private void OnEnable()
    {
        input.Enable();

        input.Cube.Drag.performed += OnDrag;
        input.Cube.Drag.canceled += OnDrag;
    }

    private void OnDisable()
    {
        if (dragging && activeRotator != null)
            activeRotator.CancelDrag();
        dragging = false;

        input.Cube.Drag.performed -= OnDrag;
        input.Cube.Drag.canceled -= OnDrag;

        input.Disable();
    }

    private void Update()
    {
        if (dragging && activeRotator != null)
        {
            activeRotator.UpdateDrag(Mouse.current.position.ReadValue());
        }
    }

    private void OnDrag(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            BeginDrag();
        }
        else if (context.canceled)
        {
            if (dragging && activeRotator != null)
            {
                activeRotator.EndDrag(Mouse.current.position.ReadValue());
            }
            dragging = false;
        }
    }

    private void BeginDrag()
    {
        if (orbitCamera == null)
            return;

        // Read the pointer at press time; Look may not have fired before the click.
        Ray ray = orbitCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, cubeLayer))
            return;

        BlockNode block =
            hit.collider.GetComponentInParent<BlockNode>();

        if (block == null)
        {
            return;
        }

        if (activeRotator == null)
        {
            Debug.LogError("Missing PuzzleDragRotator reference");
            return;
        }

        dragging = activeRotator.BeginDrag(block, hit.normal, orbitCamera,
            Mouse.current.position.ReadValue());

    }
}
