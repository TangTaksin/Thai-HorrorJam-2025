using UnityEngine;

public class SOAudioManager : MonoBehaviour
{
    public static SOAudioManager Instance { get; private set; }
    public AudioSource musicSource;
    public AudioSource ambientSource;
    public AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    public void PlayMusic(AudioEvent audioEvent)
    {
        if (audioEvent == null || musicSource == null) return;

        audioEvent.Play(musicSource);
    }

    public void PlayAmbient(AudioEvent audioEvent)
    {
        if (audioEvent == null || ambientSource == null) return;

        audioEvent.Play(ambientSource);
    }

    public void PlaySFX(AudioEvent audioEvent)
    {
        if (audioEvent == null || sfxSource == null) return;

        audioEvent.PlayOneShot(sfxSource);
    }
    
    public void PlayFootstep(FootstepAudioEvent footstep, SurfaceType surface)
    {
        if (footstep == null || sfxSource == null) return;

        footstep.PlayOneShot(sfxSource, surface);
    }
    
    public void StopMusic()
    {
        if (musicSource == null) return;

        musicSource.Stop();
    }
}
