using UnityEngine;

// Geometry shared by drag interpretation and its visual indicator.
public struct CubeDragFace
{
    private const float IndicatorSurfaceOffset = 0.015f;

    public Quaternion BlockRotation { get; private set; }
    public Quaternion IndicatorRotation { get; private set; }
    public Vector3 LocalNormal { get; private set; }
    public Vector3 LocalRight { get; private set; }
    public Vector3 LocalUp { get; private set; }
    public Vector3 IndicatorCenterWorld { get; private set; }
    public float SizeWorld { get; private set; }

    public static bool TryCreate(BlockNode block, Vector3 hitNormalWorld,
        out CubeDragFace face)
    {
        face = default;

        BoxCollider cubeCollider = block.GetComponent<BoxCollider>();
        if (cubeCollider == null)
            return false;

        // Resolve the hit to one of the cube's six local faces.
        Quaternion blockRotation = block.transform.rotation;
        Vector3 localNormal = RoundToAxis(
            Quaternion.Inverse(blockRotation) * hitNormalWorld);
        GetFaceBasis(localNormal, out Vector3 localRight, out Vector3 localUp);

        Vector3 worldNormal = block.transform.TransformDirection(localNormal);
        Vector3 localCenter = cubeCollider.center +
            Vector3.Scale(localNormal, cubeCollider.size) * 0.5f;

        // Use the two dimensions along the face to size the marker uniformly.
        Vector3 scale = block.transform.lossyScale;
        Vector3 size = Vector3.Scale(cubeCollider.size, new Vector3(
            Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        float faceSize = Mathf.Abs(localNormal.x) > 0.5f
            ? Mathf.Min(size.y, size.z)
            : Mathf.Abs(localNormal.y) > 0.5f
                ? Mathf.Min(size.x, size.z)
                : Mathf.Min(size.x, size.y);

        face = new CubeDragFace
        {
            BlockRotation = blockRotation,
            IndicatorRotation = Quaternion.LookRotation(worldNormal,
                block.transform.TransformDirection(localUp)),
            LocalNormal = localNormal,
            LocalRight = localRight,
            LocalUp = localUp,
            IndicatorCenterWorld = block.transform.TransformPoint(localCenter) +
                worldNormal * faceSize * IndicatorSurfaceOffset,
            SizeWorld = faceSize
        };

        return true;
    }

    private static void GetFaceBasis(Vector3 localNormal,
        out Vector3 faceRight, out Vector3 faceUp)
    {
        Vector3 reference = Mathf.Abs(localNormal.y) < 0.9f
            ? Vector3.up
            : Vector3.forward;

        faceRight = Vector3.Cross(reference, localNormal).normalized;
        faceUp = Vector3.Cross(localNormal, faceRight).normalized;
    }

    private static Vector3 RoundToAxis(Vector3 value)
    {
        if (Mathf.Abs(value.x) > 0.9f)
            return new Vector3(Mathf.Sign(value.x), 0f, 0f);

        if (Mathf.Abs(value.y) > 0.9f)
            return new Vector3(0f, Mathf.Sign(value.y), 0f);

        return new Vector3(0f, 0f, Mathf.Sign(value.z));
    }
}
