using UnityEngine;

public class OneWayPlatformTrigger : MonoBehaviour
{
    [SerializeField] private OneWayPlatform platform;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null)
            return;

        platform.IgnoreCollision(other, true);
        Debug.Log("Ignore collision"); 

    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        platform.IgnoreCollision(other, false);
        Debug.Log("Restore collision"); 
    }
}