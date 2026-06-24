using UnityEngine;

public class StickToWall : MonoBehaviour
{
    [SerializeField] private float rayDistance = 1f;
    [SerializeField] private LayerMask surfaceMask = ~0;
    [SerializeField] private GameObject player;
    private BlockNode currStickTarget;
    private void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        Debug.DrawRay(
            transform.position,
            transform.forward * rayDistance,
            Color.red,
            1f
            );

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, surfaceMask))
        {
            BlockNode hitObject = hit.transform.gameObject.GetComponentInParent<BlockNode>();
            if (hitObject != currStickTarget)
            {
                currStickTarget = hitObject;
                player.transform.SetParent(currStickTarget.transform, true);
            }
        }
        else
        {
            if (currStickTarget != null)
            {
                currStickTarget = null;
                player.transform.SetParent(null, true);
            }
        }
    }
}
