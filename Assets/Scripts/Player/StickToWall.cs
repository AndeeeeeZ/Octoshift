using UnityEngine;

// NOTE: should be disabled during rotation to prevent re-parenting mid rotation
public class StickToWall : MonoBehaviour
{
    [SerializeField] private float rayDistance = 1f;
    [SerializeField] private LayerMask surfaceMask = ~0;
    [SerializeField] private GameObject player;
    [SerializeField] private VoidEvent OnReachingNewBlock; 
    private BlockNode currStickTarget;
    private bool suppressAutoFocusOnNextDetection;

    public bool SuppressAutoFocus => suppressAutoFocusOnNextDetection;

    private void OnDisable()
    {
        // Rotation temporarily disables this detector while the player stays on its block.
        suppressAutoFocusOnNextDetection = currStickTarget != null;
    }

    private void LateUpdate()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, surfaceMask))
        {
            BlockNode hitObject = hit.transform.gameObject.GetComponentInParent<BlockNode>();
            if (hitObject != currStickTarget)
            {
                // Debug.Log("New block detected"); 
                currStickTarget = hitObject;
                player.transform.SetParent(currStickTarget.transform, true);
                OnReachingNewBlock.Raise(); 
            }

            suppressAutoFocusOnNextDetection = false;
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

    public Vector3 GetCurrStickTargetPosition()
    {
        if (currStickTarget == null) return Vector3.zero; 
        return currStickTarget.transform.position; 
    }
}
