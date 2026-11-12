using System.Collections.Generic;
using UnityEngine;

public class PuzzleDragRotator : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private MessageController messageController; 
    [SerializeField] private FloatValue dragThreshold;
    [SerializeField] private RegionRotator regionRotator;
    [SerializeField] private PlayerMovement playerMovement; 

    [Header("Drag Feedback")]
    [SerializeField] private CubeDragFeedbackSettings feedbackSettings;

    // The selected face defines the input plane and indicator placement.
    private BlockNode selectedBlock;
    private CubeDragFace dragFace;
    private Camera dragCamera;
    private Vector2 pressPosition;
    private bool dragging;
    private bool hasDirection;
    private bool hasDragged;

    // A direction becomes a 90-degree rotation only when the mouse is released.
    private Vector3 worldAxis;
    private int rotationDirection;
    private CubeDragFeedback dragFeedback;

    private float CancelRadiusPixels => Mathf.Max(1f, feedbackSettings.CancelRadiusPixels);

    private void Update()
    {
        dragFeedback?.Tick(Time.deltaTime);
    }

    private void OnDisable()
    {
        CancelDrag();
        dragFeedback?.HideImmediately();
    }

    private void OnDestroy()
    {
        dragFeedback?.Dispose();
    }

    public bool BeginDrag(
        BlockNode block,
        Vector3 hitNormalWorld,
        Camera camera,
        Vector2 mousePosition)
    {
        if (regionRotator.IsRotating)
            return false;

        if (feedbackSettings == null)
        {
            Debug.LogError("Missing CubeDragFeedbackSettings reference.", this);
            return false;
        }

        if (block.HasConnectionOnFace(hitNormalWorld))
        {
            messageController.DisplayMessage("This face is connected to another cube!");
            return false;
        }

        if (!CubeDragFace.TryCreate(block, hitNormalWorld, out CubeDragFace face))
        {
            Debug.LogError("A draggable cube needs a BoxCollider.", block);
            return false;
        }

        // Record the selected cube and face before showing the indicator.
        selectedBlock = block;
        dragFace = face;
        dragCamera = camera;
        pressPosition = mousePosition;
        dragging = true;
        hasDirection = false;
        rotationDirection = 0;
        hasDragged = false;
        worldAxis = Vector3.zero;

        dragFeedback ??= new CubeDragFeedback(feedbackSettings);
        dragFeedback.Show(dragFace, camera, CancelRadiusPixels,
            block.GetComponent<Renderer>());

        return true;
    }

    public void UpdateDrag(Vector2 mousePosition)
    {
        if (!dragging)
            return;

        Vector3 screenCenter = dragCamera.WorldToScreenPoint(dragFace.IndicatorCenterWorld);
        Vector2 fromCenter = mousePosition -
            new Vector2(screenCenter.x, screenCenter.y);

        if (screenCenter.z <= 0f ||
            fromCenter.sqrMagnitude <= CancelRadiusPixels * CancelRadiusPixels)
        {
            ClearPlannedRotation();
            return;
        }

        // The press position only distinguishes a drag from a click.
        if (!hasDragged)
        {
            if ((mousePosition - pressPosition).sqrMagnitude <
                dragThreshold.Value * dragThreshold.Value)
                return;

            hasDragged = true;
        }

        Vector3 worldDrag =
            dragCamera.transform.right * fromCenter.x +
            dragCamera.transform.up * fromCenter.y;

        Vector3 localDrag =
            Quaternion.Inverse(dragFace.BlockRotation) *
            worldDrag;

        Vector3 localDragOnFace =
            Vector3.ProjectOnPlane(
                localDrag,
                dragFace.LocalNormal);

        if (localDragOnFace.magnitude < dragThreshold.Value)
        {
            ClearPlannedRotation();
            return;
        }

        UpdateDirection(localDragOnFace,
            fromCenter.magnitude - CancelRadiusPixels);
    }

    private void ClearPlannedRotation()
    {
        hasDirection = false;
        rotationDirection = 0;
        worldAxis = Vector3.zero;
        dragFeedback?.CancelDirection();
    }

    public void EndDrag(Vector2 mousePosition)
    {
        if (!dragging)
            return;

        UpdateDrag(mousePosition);

        dragging = false;
        dragFeedback?.Hide();

        if (!hasDirection)
            return;

        Axis axis = ToAxis(worldAxis);

        if (!playerMovement.IsAtStablePosition())
        {
            Debug.LogWarning("Can not rotate when the player is not at a stable position"); 
            return; 
        }

        List<BlockNode> region =
            GraphTraversal.FindMovingRegion(
                selectedBlock,
                axis);

        // Check if the current rotating is going to cause two blocks to collide or overlap 
        if (!CanRotate(
                region,
                selectedBlock.transform.position,
                worldAxis,
                rotationDirection * 90f))
        {
            Debug.LogWarning("The current rotate will result in a collision"); 
            messageController.DisplayMessage("This rotation will cause a collision!"); 
            return; 
        }

        regionRotator.RotateRegion(
            region,
            selectedBlock.transform.position,
            worldAxis,
            rotationDirection * 90f);
    }

    public void CancelDrag()
    {
        dragging = false;
        hasDirection = false;
        dragFeedback?.Hide();
    }

    private bool CanRotate(
        List<BlockNode> region,
        Vector3 pivot,
        Vector3 axis,
        float angle)
    {
        HashSet<Vector3Int> occupied = new();

        // All blocks NOT in moving region
        BlockNode[] allBlocks =
            FindObjectsByType<BlockNode>(
                FindObjectsSortMode.None);

        foreach (BlockNode block in allBlocks)
        {
            if (region.Contains(block))
                continue;

            occupied.Add(
                Vector3Int.RoundToInt(
                    block.transform.position));
        }

        Quaternion rotation =
            Quaternion.AngleAxis(angle, axis);

        foreach (BlockNode block in region)
        {
            Vector3 offset =
                block.transform.position - pivot;

            Vector3 rotated =
                rotation * offset;

            Vector3 targetPos =
                pivot + rotated;

            Vector3Int gridPos =
                Vector3Int.RoundToInt(targetPos);

            if (occupied.Contains(gridPos))
                return false;
        }

        return true;
    }

    private void UpdateDirection(Vector3 localDragOnFace, float dragStrength)
    {
        float rightAmount = Vector3.Dot(localDragOnFace, dragFace.LocalRight);

        float upAmount = Vector3.Dot(localDragOnFace, dragFace.LocalUp);

        float dragAmount;
        Vector3 tangentLocal;

        if (Mathf.Abs(rightAmount) > Mathf.Abs(upAmount))
        {
            tangentLocal = dragFace.LocalRight;

            dragAmount = rightAmount;
        }
        else
        {
            tangentLocal = dragFace.LocalUp;

            dragAmount = upAmount;
        }

        worldAxis = (
                dragFace.BlockRotation *
                Vector3.Cross(dragFace.LocalNormal, tangentLocal)).normalized;

        rotationDirection = dragAmount >= 0f ? 1 : -1;

        hasDirection = true;
        dragFeedback?.ShowDirection(tangentLocal * rotationDirection,
            dragStrength);
    }

    private static Axis ToAxis(Vector3 axis)
    {
        axis = axis.normalized;

        if (Mathf.Abs(axis.x) > 0.9f)
            return Axis.X;

        if (Mathf.Abs(axis.y) > 0.9f)
            return Axis.Y;

        return Axis.Z;
    }
}
