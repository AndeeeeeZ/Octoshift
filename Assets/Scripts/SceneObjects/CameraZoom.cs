using UnityEngine;
using UnityEngine.InputSystem;

// Scroll mouse wheel to change the size of orthographic camera

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private float minZoom;
    [SerializeField] private float maxZoom;
    [SerializeField] private float zoomSensitivity;

    private PlayerInput input;
    private Camera cam;

    private void Awake()
    {
        input = new PlayerInput();
        cam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        input.Enable();
        input.Camera.Zoom.performed += ChangeZoom;
    }

    private void OnDisable()
    {
        input.Camera.Zoom.performed -= ChangeZoom;
        input.Disable();
    }

    private void ChangeZoom(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<Vector2>().y;

        cam.orthographicSize -= scroll * zoomSensitivity;

        cam.orthographicSize = Mathf.Clamp(
            cam.orthographicSize,
            minZoom,
            maxZoom);
    }
}