using System.Collections.Generic;
using UnityEngine;

public class RegionRotator : MonoBehaviour
{
    [SerializeField] private FloatValue snapDuration;

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

        startRotation = pivot.rotation;

        targetRotation =
            Quaternion.AngleAxis(angle, worldAxis) *
            startRotation;

        timer = 0f;
        rotating = true;
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
    }

    public bool IsRotating => rotating;
}