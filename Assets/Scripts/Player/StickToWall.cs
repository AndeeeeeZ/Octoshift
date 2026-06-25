using UnityEngine;

// NOTE: should be disabled during rotation to prevent re-parenting mid rotation
public class StickToWall : MonoBehaviour
{
    [SerializeField] private float rayDistance = 1f;
    [SerializeField] private LayerMask surfaceMask = ~0;
    [SerializeField] private GameObject player;
    private BlockNode currStickTarget;
    private void LateUpdate()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        // Debug.DrawRay(
        //     transform.position,
        //     transform.forward * rayDistance,
        //     Color.red,
        //     1f
        //     );

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, surfaceMask))
        {
            BlockNode hitObject = hit.transform.gameObject.GetComponentInParent<BlockNode>();
            if (hitObject != currStickTarget)
            {
                Debug.Log("New block detected"); 
                currStickTarget = hitObject;
                player.transform.SetParent(currStickTarget.transform, true);
            }
        }
        else
        {
            if (currStickTarget == null) 
                return; 
                
            Debug.Log("No block detected"); 
            if (currStickTarget != null)
            {
                currStickTarget = null;
                player.transform.SetParent(null, true);
            }
        }
    }

    public bool IsOnABlock()
    {
        return currStickTarget != null; 
    }
}
