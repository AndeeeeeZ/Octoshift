using System.Collections.Generic;
using UnityEngine;

// Keeps movable puzzle geometry in front of an orthographic camera's near plane.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public class CameraNearClipGuard : MonoBehaviour
{
    [SerializeField] private float clearance = 1f;

    private readonly List<Renderer> puzzleRenderers = new();
    private Camera cam;
    private Vector3 restingLocalPosition;
    private float restingFarClipPlane;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        restingLocalPosition = transform.localPosition;
        restingFarClipPlane = cam.farClipPlane;
    }

    private void Start()
    {
        // The puzzle's blocks and poles can be reparented while rotating, so
        // keep their renderers rather than searching the hierarchy every frame.
        foreach (BlockNode block in FindObjectsByType<BlockNode>(FindObjectsSortMode.None))
            puzzleRenderers.AddRange(block.GetComponentsInChildren<Renderer>(true));

        foreach (Pole pole in FindObjectsByType<Pole>(FindObjectsSortMode.None))
            puzzleRenderers.AddRange(pole.GetComponentsInChildren<Renderer>(true));
    }

    private void LateUpdate()
    {
        if (!cam.orthographic)
            return;

        transform.localPosition = restingLocalPosition;

        Vector3 forward = transform.forward;
        Vector3 position = transform.position;
        Vector3 absoluteForward = new(
            Mathf.Abs(forward.x),
            Mathf.Abs(forward.y),
            Mathf.Abs(forward.z));

        float retreat = 0f;
        float farthestDepth = 0f;

        foreach (Renderer renderer in puzzleRenderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = renderer.bounds;
            float centerDepth = Vector3.Dot(bounds.center - position, forward);
            float depthRadius = Vector3.Dot(bounds.extents, absoluteForward);

            retreat = Mathf.Max(retreat,
                cam.nearClipPlane + clearance - (centerDepth - depthRadius));
            farthestDepth = Mathf.Max(farthestDepth, centerDepth + depthRadius);
        }

        transform.position = position - forward * retreat;
        cam.farClipPlane = Mathf.Max(restingFarClipPlane,
            farthestDepth + retreat + clearance);
    }
}
