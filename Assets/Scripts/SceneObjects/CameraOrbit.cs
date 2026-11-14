using System;
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
    [SerializeField] private bool allowManualControls = true;

    [Header("Focus")]
    [SerializeField] private float focusMoveSpeed = 8f;
    [SerializeField] private float focusRotateSpeed = 8f;
    [SerializeField] private FloatValue focusAngleOffsetX, focusAngleOffsetY;
    [SerializeField] private bool perpendicularFocus;
    [Tooltip("Shift the focus upward along the player's up direction. Positive values show more above the player.")]
    [SerializeField] private float focusHeightOffset = 0f;

    [Header("Scripted Focus")]
    [Tooltip("Speed multiplier for FocusOn moves, such as tutorial pole focuses.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float scriptedFocusSpeedMultiplier = 0.3f;
    [Tooltip("Return focus to the player after they move this far past a scripted target.")]
    [Min(0.1f)]
    [SerializeField] private float scriptedFocusPassDistance = 4.5f;

    private PlayerInput input;

    private Vector2 lookInput;
    private bool rotating;
    private Vector3 orbitUp;

    private bool panning;
    private Plane panPlane;
    private Vector3 lastPanPoint;

    private Camera cam;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool focusing;
    private float activeFocusSpeedMultiplier = 1f;
    private Transform scriptedFocusTarget;
    private Vector3 scriptedFocusAxis;

    private PlayerMovement playerMovement;
    private StickToWall stickToWall;

    public event Action<float> OrbitPerformed;
    public event Action<float> PanPerformed;
    public event Action FocusPerformed;

    public bool IsFocusing => focusing;

    private void Awake()
    {
        input = new PlayerInput();
        cam = Camera.main;
        playerMovement = player.GetComponent<PlayerMovement>();
        stickToWall = player.GetComponent<StickToWall>();
    }

    private void Start()
    {
        targetPosition = orbitPivot.position;
        targetRotation = orbitPivot.rotation;

        FocusOnCurrentBlock();
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
        if (!allowManualControls)
            return;

        rotating = context.ReadValueAsButton();

        if (rotating)
        {
            // Use the current view as the reference for this drag, including after panning.
            orbitUp = orbitPivot.up;
            focusing = false;
            scriptedFocusTarget = null;
        }
    }

    private void OnPan(InputAction.CallbackContext context)
    {
        if (!allowManualControls)
            return;

        panning = context.ReadValueAsButton();

        if (!panning)
            return;

        // Cancel focus if the player manually pans the camera
        focusing = false;
        scriptedFocusTarget = null;

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
        if (!allowManualControls)
            return;

        FocusOnCurrentBlock();
        FocusPerformed?.Invoke();
    }

    public void SetManualControlsEnabled(bool value)
    {
        allowManualControls = value;

        // A scripted camera move must also stop any drag already in progress.
        if (!value)
        {
            rotating = false;
            panning = false;
            lookInput = Vector2.zero;
        }
    }

    public void SetPerpendicularFocus(bool value)
    {
        perpendicularFocus = value;
    }

    public void FocusOnCurrentBlock(bool immediately = false)
    {
        SetFaceFocus(playerMovement.GetCurrBlockPosition());

        if (immediately)
        {
            orbitPivot.SetPositionAndRotation(targetPosition, targetRotation);
            focusing = false;
        }
    }

    public void Focus()
    {
        if (!isActiveAndEnabled || stickToWall == null || stickToWall.SuppressAutoFocus)
            return;

        FocusOnCurrentBlock();
    }

    public void FocusOn(Transform focusTarget)
    {
        if (!isActiveAndEnabled || focusTarget == null)
            return;

        scriptedFocusAxis = player.right;
        if (HasPassedScriptedTarget(focusTarget))
        {
            FocusOnCurrentBlock();
            return;
        }

        // Use the same focus target so subsequent block autofocus can take over.
        SetFaceFocus(focusTarget.position, scriptedFocusSpeedMultiplier);
        scriptedFocusTarget = focusTarget;
    }

    private void SetFaceFocus(Vector3 position, float speedMultiplier = 1f)
    {
        scriptedFocusTarget = null;

        Quaternion playerRotation = Quaternion.LookRotation(
            player.forward,
            player.up);

        Quaternion offset = perpendicularFocus ? Quaternion.identity : Quaternion.Euler(
            focusAngleOffsetX.Value,
            focusAngleOffsetY.Value,
            0f);

        targetPosition = position + player.up * focusHeightOffset;
        targetRotation = playerRotation * offset;

        // Each request owns its speed; block and manual focus restore full speed.
        activeFocusSpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
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

        Vector3 right = orbitPivot.right;

        Quaternion yaw = Quaternion.AngleAxis(
            lookInput.x * sensitivity,
            orbitUp);

        Quaternion pitch = Quaternion.AngleAxis(
            -lookInput.y * sensitivity,
            right);

        Quaternion previousRotation = orbitPivot.rotation;
        orbitPivot.rotation =
            yaw * pitch * orbitPivot.rotation;

        float angle = Quaternion.Angle(previousRotation, orbitPivot.rotation);
        if (angle > 0f)
            OrbitPerformed?.Invoke(angle);
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

        float distance = (delta * panSensitivity).magnitude;
        if (distance > 0f)
            PanPerformed?.Invoke(distance);

        ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (panPlane.Raycast(ray, out enter))
        {
            lastPanPoint = ray.GetPoint(enter);
        }
    }

    private void HandleFocus()
    {
        if (scriptedFocusTarget != null && HasPassedScriptedTarget(scriptedFocusTarget))
        {
            // Stop following an old tutorial target after the player has passed it.
            FocusOnCurrentBlock();
        }

        if (!focusing)
            return;

        // Exponential smoothing for consistent movement
        float moveT = 1f - Mathf.Exp(-focusMoveSpeed * activeFocusSpeedMultiplier * Time.deltaTime);
        float rotateT = 1f - Mathf.Exp(-focusRotateSpeed * activeFocusSpeedMultiplier * Time.deltaTime);

        orbitPivot.position = Vector3.Lerp(
            orbitPivot.position,
            targetPosition,
            moveT);

        // A perpendicular view must not tilt between surface orientations.
        orbitPivot.rotation = perpendicularFocus ? targetRotation : Quaternion.Slerp(
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

    private bool HasPassedScriptedTarget(Transform focusTarget)
    {
        return Vector3.Dot(player.position - focusTarget.position, scriptedFocusAxis)
            > scriptedFocusPassDistance;
    }
}
