using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [SerializeField] private Collider platformCollider;

    public void IgnoreCollision(Collider player, bool ignore)
    {
        Physics.IgnoreCollision(player, platformCollider, ignore);
    }
}