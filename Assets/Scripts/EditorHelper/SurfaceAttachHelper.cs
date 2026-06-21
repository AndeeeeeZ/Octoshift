using UnityEngine;

public class SurfaceAttachHelper : MonoBehaviour
{
    [SerializeField] private float surfaceOffset = 0.01f;
    [SerializeField] private float rayDistance = 5f;
    [SerializeField] private LayerMask surfaceMask = ~0;
    [SerializeField] private bool alignToSurface = true;

    public void SnapToSurface()
    {
        Ray ray = new Ray(
            transform.position,
            transform.forward);

        Debug.DrawRay(
            transform.position,
            transform.forward * rayDistance,
            Color.red,
            1f);

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            rayDistance,
            surfaceMask))
        {
            transform.position =
                hit.point +
                hit.normal * surfaceOffset;

            if (alignToSurface)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        -hit.normal,
                        transform.up);
            }
        }
        else
        {
            Debug.LogWarning(
                $"No surface found for {name}");
        }
    }
}