using UnityEngine;

public class Bound : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private VoidEvent OnPlayerFellOutsideOfScreen;
    private void LateUpdate()
    {
        transform.position = target.position;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player is outside of screen");
            OnPlayerFellOutsideOfScreen.Raise();
        }
    }
}
