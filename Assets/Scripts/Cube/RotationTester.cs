using System.Collections.Generic;
using UnityEngine;

public class RotationTester : MonoBehaviour
{
    [SerializeField] private BlockNode selectedBlock;
    [SerializeField] private Axis axis;

    [SerializeField] private RegionRotator regionRotator;

    [ContextMenu("Rotate")]
    private void Rotate()
    {
        List<BlockNode> region =
            GraphTraversal.FindMovingRegion(
                selectedBlock,
                axis);

        Vector3 worldAxis = axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            _ => Vector3.forward
        };

        regionRotator.RotateRegion(
            region,
            selectedBlock.transform.position,
            worldAxis,
            90f);
    }
}