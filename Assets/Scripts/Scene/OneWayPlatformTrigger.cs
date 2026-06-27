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

        // Moving toward the back of the platform?
        bool passingUp =
            Vector3.Dot(rb.linearVelocity, platform.transform.up) > 0f;

        if (passingUp)
        {
            platform.IgnoreCollision(other, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        platform.IgnoreCollision(other, false);
    }
}