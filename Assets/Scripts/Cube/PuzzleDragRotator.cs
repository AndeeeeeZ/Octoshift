using System.Collections.Generic;
using UnityEngine;

public class PuzzleDragRotator : MonoBehaviour
{
    [SerializeField] private FloatValue dragThreshold;

    [SerializeField] private RegionRotator regionRotator;

    private BlockNode selectedBlock;

    private bool dragging;
    private bool axisLocked;

    private Vector3 localFaceNormal;
    private Vector3 faceRightLocal;
    private Vector3 faceUpLocal;

    private Vector3 worldAxis;
    private Vector3 chosenTangentLocal;

    private Transform dragCamera;

    private Quaternion startRotation;

    private int rotationDirection;

    public bool BeginDrag(
        BlockNode block,
        Vector3 hitNormalWorld,
        Transform cameraTransform)
    {
        if (regionRotator.IsRotating)
            return false;

        selectedBlock = block;

        dragCamera = cameraTransform;

        dragging = true;
        axisLocked = false;

        rotationDirection = 0;

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
        
        return true; 
    }

    public void UpdateDrag(Vector2 mouseDelta)
    {
        if (!dragging)
            return;

        if (axisLocked)
            return;

        if (mouseDelta.sqrMagnitude <= 0.000001f)
            return;

        Vector3 worldDrag =
            dragCamera.right * mouseDelta.x +
            dragCamera.up * mouseDelta.y;

        Vector3 localDrag =
            Quaternion.Inverse(startRotation) *
            worldDrag;

        Vector3 localDragOnFace =
            Vector3.ProjectOnPlane(
                localDrag,
                localFaceNormal);

        if (localDragOnFace.magnitude <
            dragThreshold.Value * 0.001f)
            return;

        LockAxis(localDragOnFace);
    }

    public void EndDrag()
    {
        if (!dragging)
            return;

        dragging = false;

        if (!axisLocked)
            return;

        Axis axis = ToAxis(worldAxis);

        List<BlockNode> region =
            GraphTraversal.FindMovingRegion(
                selectedBlock,
                axis);

        regionRotator.RotateRegion(
            region,
            selectedBlock.transform.position,
            worldAxis,
            rotationDirection * 90f);
    }

    private void LockAxis(Vector3 localDragOnFace)
    {
        float rightAmount =
            Vector3.Dot(
                localDragOnFace,
                faceRightLocal);

        float upAmount =
            Vector3.Dot(
                localDragOnFace,
                faceUpLocal);

        float dragAmount;

        if (Mathf.Abs(rightAmount) >
            Mathf.Abs(upAmount))
        {
            chosenTangentLocal =
                faceRightLocal;

            dragAmount =
                rightAmount;
        }
        else
        {
            chosenTangentLocal =
                faceUpLocal;

            dragAmount =
                upAmount;
        }

        worldAxis =
            (
                startRotation *
                Vector3.Cross(
                    localFaceNormal,
                    chosenTangentLocal)
            ).normalized;

        rotationDirection =
            dragAmount >= 0f
            ? 1
            : -1;

        axisLocked = true;
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