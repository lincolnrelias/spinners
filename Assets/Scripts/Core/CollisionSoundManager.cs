using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CollisionSoundManager : MonoBehaviour
{
    public static CollisionSoundManager Instance { get; private set; }

    private AudioSource audioSource;
    private float lastSpinnerSoundTime = -1f;
    private float lastWallSoundTime = -1f;
    private const float MinTimeBetweenSounds = 0.05f;

    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlaySpinnerCollision(AudioClip clip)
    {
        if (clip == null) return;
        if (Time.time - lastSpinnerSoundTime < MinTimeBetweenSounds) return;
        lastSpinnerSoundTime = Time.time;
        audioSource.PlayOneShot(clip);
    }

    public void PlayWallCollision(AudioClip clip)
    {
        if (clip == null) return;
        if (Time.time - lastWallSoundTime < MinTimeBetweenSounds) return;
        lastWallSoundTime = Time.time;
        audioSource.PlayOneShot(clip);
    }
}
