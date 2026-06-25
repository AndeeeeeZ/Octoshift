using UnityEngine;

public class SoundEffectPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] audioClips;
    [SerializeField] private Vector2 randomPitchRange, randomVolumeRange;
    [SerializeField] private float startTime; 
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public void PlayClip()
    {
        if (audioClips.Length == 0)
        {
            Debug.LogError("Missing audio clip"); 
            return; 
        }

        AudioClip clip = audioClips[Random.Range(0, audioClips.Length)]; 

        audioSource.pitch = Random.Range(randomPitchRange.x, randomPitchRange.y);
        audioSource.volume = Random.Range(randomVolumeRange.x, randomVolumeRange.y);
        audioSource.time = startTime; 
        audioSource.PlayOneShot(clip);
    }
}
