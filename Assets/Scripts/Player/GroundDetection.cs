using UnityEngine;

// Three point ground detection
public class GroundDetection : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float checkDistance;
    [SerializeField] private float checkRadius;
    [SerializeField] private float horizontalOffset; 

    public bool IsGrounded()
    {
        Vector3 center = transform.position - transform.up * checkDistance;
        Vector3 left = center - transform.right * horizontalOffset;
        Vector3 right = center + transform.right * horizontalOffset;

        return Physics.CheckSphere(center, checkRadius, groundLayer)
            || Physics.CheckSphere(left, checkRadius, groundLayer)
            || Physics.CheckSphere(right, checkRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position - transform.up * checkDistance;
        Vector3 left = center - transform.right * horizontalOffset;
        Vector3 right = center + transform.right * horizontalOffset;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(left, checkRadius);
        Gizmos.DrawWireSphere(center, checkRadius);
        Gizmos.DrawWireSphere(right, checkRadius);
    }
}