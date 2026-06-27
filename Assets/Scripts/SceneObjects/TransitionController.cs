using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TransitionController : MonoBehaviour
{
    [SerializeField] private PlayableDirector transitionOut; 
    [SerializeField] private UnityEvent OnTransitionOutEnd; 

    public void TransitionOutEnds()
    {
        OnTransitionOutEnd?.Invoke(); 
    }

    public void PlayTransitionOut()
    {
        transitionOut.Play(); 
    }
}
