using UnityEngine;
using UnityEngine.InputSystem;

public class CubeDragRotation : MonoBehaviour
{
    [SerializeField] private Camera orbitCamera;
    [SerializeField] private float dragThreshold = 1f;
    [SerializeField] private float rotationSensitivity = 0.5f;
    [SerializeField] private float snapSpeed = 360f;

    private PlayerInput input;

    private Vector2 lookInput;
    private Vector2 accumulatedDrag;

    private bool dragging;
    private bool snapping;

    private Quaternion dragStartRotation;
    private Quaternion snapTargetRotation;

    private float currentAngle;

    private Vector3 lockedAxis = Vector3.zero;

    private void Awake()
    {
        input = new PlayerInput();
    }

    private void OnEnable()
    {
        input.Enable();

        input.Cube.Drag.performed += OnDrag;
        input.Cube.Drag.canceled += OnDrag;

        input.Cube.Look.performed += OnLook;
        input.Cube.Look.canceled += OnLook;
    }

    private void OnDisable()
    {
        input.Cube.Drag.performed -= OnDrag;
        input.Cube.Drag.canceled -= OnDrag;

        input.Cube.Look.performed -= OnLook;
        input.Cube.Look.canceled -= OnLook;

        input.Disable();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnDrag(InputAction.CallbackContext context)
    {
        bool pressed = context.ReadValueAsButton();

        if (pressed)
        {
            dragging = true;
            snapping = false;

            accumulatedDrag = Vector2.zero;
            currentAngle = 0f;

            lockedAxis = Vector3.zero;
            dragStartRotation = transform.rotation;
        }
        else
        {
            dragging = false;

            if (lockedAxis == Vector3.zero)
                return;

            float snappedAngle =
                Mathf.Round(currentAngle / 90f) * 90f;

            snapTargetRotation =
                Quaternion.AngleAxis(
                    snappedAngle,
                    lockedAxis)
                * dragStartRotation;

            snapping = true;
        }
    }

    private void Update()
    {
        if (dragging)
        {
            HandleDragging();
        }

        if (snapping)
        {
            HandleSnap();
        }
    }

    private void HandleDragging()
    {
        accumulatedDrag += lookInput;

        if (lockedAxis == Vector3.zero)
        {
            if (accumulatedDrag.magnitude < dragThreshold)
                return;

            bool horizontal =
                Mathf.Abs(accumulatedDrag.x) >
                Mathf.Abs(accumulatedDrag.y);

            if (horizontal)
            {
                lockedAxis =
                    ClosestWorldAxis(
                        orbitCamera.transform.up);

                currentAngle =
                    -accumulatedDrag.x *
                    rotationSensitivity;
            }
            else
            {
                lockedAxis =
                    ClosestWorldAxis(
                        orbitCamera.transform.right);

                currentAngle =
                    accumulatedDrag.y *
                    rotationSensitivity;
            }
        }
        else
        {
            bool horizontal =
                Mathf.Abs(accumulatedDrag.x) >
                Mathf.Abs(accumulatedDrag.y);

            if (horizontal)
            {
                currentAngle =
                    -accumulatedDrag.x *
                    rotationSensitivity;
            }
            else
            {
                currentAngle =
                    accumulatedDrag.y *
                    rotationSensitivity;
            }
        }

        transform.rotation =
            Quaternion.AngleAxis(
                currentAngle,
                lockedAxis)
            * dragStartRotation;
    }

    private void HandleSnap()
    {
        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                snapTargetRotation,
                snapSpeed * Time.deltaTime);

        if (Quaternion.Angle(
                transform.rotation,
                snapTargetRotation) < 0.1f)
        {
            transform.rotation =
                snapTargetRotation;

            snapping = false;
        }
    }

    private Vector3 ClosestWorldAxis(Vector3 direction)
    {
        direction.Normalize();

        Vector3[] axes =
        {
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down,
            Vector3.forward,
            Vector3.back
        };

        Vector3 bestAxis = axes[0];
        float bestDot = -Mathf.Infinity;

        foreach (Vector3 axis in axes)
        {
            float dot = Vector3.Dot(direction, axis);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestAxis = axis;
            }
        }

        return bestAxis;
    }
}