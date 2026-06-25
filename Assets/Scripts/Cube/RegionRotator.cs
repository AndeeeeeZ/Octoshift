using System.Collections.Generic;
using UnityEngine;

public class RegionRotator : MonoBehaviour
{
    [SerializeField] private FloatValue snapDuration;
    [SerializeField] private VoidEvent OnRotationStarts, OnRotationEnds;

    private readonly List<Transform> movingTransforms = new();

    private Transform pivot;

    private bool rotating;

    private Quaternion startRotation;
    private Quaternion targetRotation;

    private float timer;

    private void Awake()
    {
        GameObject pivotObject = new("Rotation Pivot");
        pivotObject.transform.SetParent(transform);

        pivot = pivotObject.transform;
    }

    private void Update()
    {
        if (!rotating)
            return;

        timer += Time.deltaTime;

        float alpha = Mathf.Clamp01(timer / snapDuration.Value);

        pivot.rotation = Quaternion.Slerp(
            startRotation,
            targetRotation,
            alpha);

        if (alpha >= 1f)
        {
            FinishRotation();
        }
    }

    // Start rotation
    public void RotateRegion(
        List<BlockNode> region,
        Vector3 pivotPosition,
        Vector3 worldAxis,
        float angle)
    {
        if (rotating)
            return;

        movingTransforms.Clear();

        pivot.position = pivotPosition;
        pivot.rotation = Quaternion.identity;

        foreach (BlockNode block in region)
        {
            movingTransforms.Add(block.transform);

            block.transform.SetParent(
                pivot,
                true);
        }

        // Find all poles that will rotate
        HashSet<Pole> polesToMove = new();

        foreach (BlockNode block in region)
        {
            foreach (Pole pole in block.Connections)
            {
                polesToMove.Add(pole);
            }
        }

        foreach (Pole pole in polesToMove)
        {
            movingTransforms.Add(pole.transform);
            pole.transform.SetParent(pivot, true);
        }

        startRotation = pivot.rotation;

        targetRotation =
            Quaternion.AngleAxis(angle, worldAxis) *
            startRotation;

        timer = 0f;
        rotating = true;

        OnRotationStarts.Raise(); 
    }

    private void FinishRotation()
    {
        pivot.rotation = targetRotation;

        foreach (Transform t in movingTransforms)
        {
            t.SetParent(null, true);
        }

        movingTransforms.Clear();

        rotating = false;

        OnRotationEnds.Raise(); 
    }

    public bool IsRotating => rotating;
}