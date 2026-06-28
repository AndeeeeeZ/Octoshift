using UnityEngine;

public class PlayClip : MonoBehaviour
{
    [SerializeField] private AudioClip clip; 
    void Start()
    {
        MusicManager.Instance.PlayMusic(clip);         
    }

}
