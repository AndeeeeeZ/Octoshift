using UnityEngine;
using UnityEngine.Events;

public class Exit : MonoBehaviour
{
    [SerializeField] private bool DEBUG; 
    public UnityEvent OnReachingExit; 
    private string playerTag = "Player";
    private bool hasReachedExit;

    private void Start()
    {
        hasReachedExit = false; 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasReachedExit) return; 
        if (other.gameObject.CompareTag(playerTag))
        {
            if (DEBUG)
                Debug.Log("Player has reached the exit"); 
            OnReachingExit?.Invoke();
            hasReachedExit = true;  
        }    
    
    }



}
