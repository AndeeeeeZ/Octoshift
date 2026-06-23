using System.Collections;
using UnityEngine;

public class CubeRotator : MonoBehaviour
{
    [SerializeField] private FloatValue dragThreshold;
    [SerializeField] private FloatValue degreesPerPixel;
    [SerializeField] private FloatValue snapDuration;

    private bool dragging;
    private bool axisLocked;

    private Vector3 localFaceNormal;
    private Vector3 faceRightLocal;
    private Vector3 faceUpLocal;

    private Vector3 worldAxis;

    private Quaternion startRotation;
    private float currentAngle;

    private Transform dragCamera;
    private Coroutine snapRoutine;

    public void BeginDrag(Vector3 hitNormalWorld, Transform cameraTransform)
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }

        dragCamera = cameraTransform;
        dragging = true;
        axisLocked = false;
        currentAngle = 0f;

        // Snap the cube before starting the drag so the math stays stable.
        startRotation = SnapToRightAngles(transform.rotation);
        transform.rotation = startRotation;

        // Convert the clicked face normal into the cube's local space.
        localFaceNormal = Quaternion.Inverse(startRotation) * hitNormalWorld;
        localFaceNormal = RoundToAxis(localFaceNormal);

        // Build a stable local basis for that face.
        GetFaceBasis(localFaceNormal, out faceRightLocal, out faceUpLocal);

        worldAxis = Vector3.zero;
    }

    public void UpdateDrag(Vector2 mouseDelta)
    {
        if (!dragging || dragCamera == null)
            return;

        if (mouseDelta.sqrMagnitude <= 0.000001f)
            return;

        // Convert screen drag into a world-space drag vector.
        Vector3 worldDrag =
            dragCamera.right * mouseDelta.x +
            dragCamera.up * mouseDelta.y;

        // Convert that drag into cube-local space.
        Vector3 localDrag = Quaternion.Inverse(startRotation) * worldDrag;

        // Keep only the part that lies on the clicked face.
        Vector3 localDragOnFace = Vector3.ProjectOnPlane(localDrag, localFaceNormal);

        if (!axisLocked)
        {
            if (localDragOnFace.magnitude < dragThreshold.Value * 0.001f)
                return;

            LockAxis(localDragOnFace);
            if (!axisLocked)
                return;
        }

        // Only use movement along the chosen face tangent.
        float dragAmount = Vector3.Dot(localDragOnFace, chosenTangentLocal);

        currentAngle += dragAmount * degreesPerPixel.Value;

        transform.rotation = Quaternion.AngleAxis(currentAngle, worldAxis) * startRotation;
    }

    public void EndDrag()
    {
        if (!dragging)
            return;

        dragging = false;

        if (!axisLocked)
        {
            transform.rotation = startRotation;
            return;
        }

        float snappedAngle = Mathf.Round(currentAngle / 90f) * 90f;
        Quaternion targetRotation =
            Quaternion.AngleAxis(snappedAngle, worldAxis) * startRotation;

        if (snapRoutine != null)
            StopCoroutine(snapRoutine);

        snapRoutine = StartCoroutine(SnapToRotation(targetRotation));
    }

    private Vector3 chosenTangentLocal;

    private void LockAxis(Vector3 localDragOnFace)
    {
        float rightAmount = Vector3.Dot(localDragOnFace, faceRightLocal);
        float upAmount = Vector3.Dot(localDragOnFace, faceUpLocal);

        // Pick the face direction that the drag is mostly following.
        if (Mathf.Abs(rightAmount) > Mathf.Abs(upAmount))
        {
            chosenTangentLocal = faceRightLocal;
            currentAngle = 0f;
            worldAxis = (startRotation * Vector3.Cross(localFaceNormal, chosenTangentLocal)).normalized;

            // Seed the angle direction from the initial drag.
            currentAngle += rightAmount * degreesPerPixel.Value;
        }
        else
        {
            chosenTangentLocal = faceUpLocal;
            currentAngle = 0f;
            worldAxis = (startRotation * Vector3.Cross(localFaceNormal, chosenTangentLocal)).normalized;

            // Seed the angle direction from the initial drag.
            currentAngle += upAmount * degreesPerPixel.Value;
        }

        axisLocked = true;
    }

    private IEnumerator SnapToRotation(Quaternion targetRotation)
    {
        Quaternion from = transform.rotation;
        float t = 0f;

        while (t < snapDuration.Value)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / snapDuration.Value);
            transform.rotation = Quaternion.Slerp(from, targetRotation, alpha);
            yield return null;
        }

        transform.rotation = targetRotation;
        startRotation = targetRotation;
        currentAngle = 0f;
        snapRoutine = null;
    }

    private static void GetFaceBasis(Vector3 localNormal, out Vector3 faceRight, out Vector3 faceUp)
    {
        // Use a stable reference so the basis does not flip around.
        Vector3 reference = Mathf.Abs(localNormal.y) < 0.9f ? Vector3.up : Vector3.forward;

        faceRight = Vector3.Cross(reference, localNormal).normalized;
        faceUp = Vector3.Cross(localNormal, faceRight).normalized;
    }

    private static Vector3 RoundToAxis(Vector3 v)
    {
        if (Mathf.Abs(v.x) > 0.9f)
            return new Vector3(Mathf.Sign(v.x), 0f, 0f);

        if (Mathf.Abs(v.y) > 0.9f)
            return new Vector3(0f, Mathf.Sign(v.y), 0f);

        return new Vector3(0f, 0f, Mathf.Sign(v.z));
    }

    private static Quaternion SnapToRightAngles(Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;

        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;

        return Quaternion.Euler(euler);
    }
}