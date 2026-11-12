using System.Collections.Generic;
using UnityEngine;

public class PuzzleDragRotator : MonoBehaviour
{
    private const float CancelRadiusPixels = 24f;

    [SerializeField] private MessageController messageController; 
    [SerializeField] private FloatValue dragThreshold;

    [SerializeField] private RegionRotator regionRotator;
    [SerializeField] private PlayerMovement playerMovement; 

    private BlockNode selectedBlock;

    private bool dragging;
    private bool axisLocked;

    private Vector3 localFaceNormal;
    private Vector3 faceRightLocal;
    private Vector3 faceUpLocal;

    private Vector3 worldAxis;
    private Vector3 chosenTangentLocal;

    private Camera dragCamera;
    private Vector3 faceCenterWorld;
    private Vector2 pressPosition;
    private bool hasDragged;

    private Quaternion startRotation;

    private int rotationDirection;
    private CubeDragFeedback dragFeedback;

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

        if (block.HasConnectionOnFace(hitNormalWorld))
        {
            messageController.DisplayMessage("This face is connected to another cube!");
            return false;
        }

        selectedBlock = block;

        dragCamera = camera;

        dragging = true;
        axisLocked = false;

        rotationDirection = 0;
        pressPosition = mousePosition;
        hasDragged = false;

        startRotation = block.transform.rotation;

        localFaceNormal =
            Quaternion.Inverse(startRotation) *
            hitNormalWorld;

        localFaceNormal = RoundToAxis(localFaceNormal);

        GetFaceBasis(
            localFaceNormal,
            out faceRightLocal,
            out faceUpLocal);

        worldAxis = Vector3.zero;

        BoxCollider cubeCollider = block.GetComponent<BoxCollider>();
        dragFeedback ??= new CubeDragFeedback();
        dragFeedback.Show(block, cubeCollider,
            localFaceNormal, faceRightLocal, faceUpLocal,
            camera, CancelRadiusPixels);
        faceCenterWorld = dragFeedback.MarkerCenterWorld;

        return true;
    }

    public void UpdateDrag(Vector2 mousePosition)
    {
        if (!dragging)
            return;

        Vector3 screenCenter = dragCamera.WorldToScreenPoint(faceCenterWorld);
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
            Quaternion.Inverse(startRotation) *
            worldDrag;

        Vector3 localDragOnFace =
            Vector3.ProjectOnPlane(
                localDrag,
                localFaceNormal);

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
        axisLocked = false;
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

        if (!axisLocked)
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
        axisLocked = false;
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
        float rightAmount = Vector3.Dot(localDragOnFace, faceRightLocal);

        float upAmount = Vector3.Dot(localDragOnFace, faceUpLocal);

        float dragAmount;

        if (Mathf.Abs(rightAmount) > Mathf.Abs(upAmount))
        {
            chosenTangentLocal = faceRightLocal;

            dragAmount = rightAmount;
        }
        else
        {
            chosenTangentLocal = faceUpLocal;

            dragAmount = upAmount;
        }

        worldAxis = (
                startRotation * Vector3.Cross(localFaceNormal, chosenTangentLocal)).normalized;

        rotationDirection = dragAmount >= 0f ? 1 : -1;

        axisLocked = true;
        dragFeedback?.ShowDirection(chosenTangentLocal * rotationDirection,
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

    private static void GetFaceBasis(
        Vector3 localNormal,
        out Vector3 faceRight,
        out Vector3 faceUp)
    {
        Vector3 reference =
            Mathf.Abs(localNormal.y) < 0.9f
            ? Vector3.up
            : Vector3.forward;

        faceRight =
            Vector3.Cross(
                reference,
                localNormal)
            .normalized;

        faceUp =
            Vector3.Cross(
                localNormal,
                faceRight)
            .normalized;
    }

    private static Vector3 RoundToAxis(Vector3 v)
    {
        if (Mathf.Abs(v.x) > 0.9f)
            return new Vector3(
                Mathf.Sign(v.x),
                0f,
                0f);

        if (Mathf.Abs(v.y) > 0.9f)
            return new Vector3(
                0f,
                Mathf.Sign(v.y),
                0f);

        return new Vector3(
            0f,
            0f,
            Mathf.Sign(v.z));
    }
}
