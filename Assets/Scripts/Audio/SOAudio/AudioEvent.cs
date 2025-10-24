using UnityEngine;

[CreateAssetMenu(menuName = "Audio/AudioEvent")]
public class AudioEvent : ScriptableObject
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f;
    public bool loop = false;

    /// <summary>
    /// เล่นเสียงแบบ loop / music
    /// </summary>
    /// <param name="audioSource">AudioSource ที่จะใช้</param>
    public virtual void Play(AudioSource audioSource)
    {
        if (clip == null || audioSource == null) return;

        // ป้องกันปัญหา clip เดิม interfere
        if (audioSource.clip != clip)
        {
            audioSource.Stop();
            audioSource.clip = clip;
        }

        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.loop = loop;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    /// <summary>
    /// เล่นเสียงแบบ SFX / one-shot
    /// </summary>
    /// <param name="audioSource">AudioSource สำหรับ SFX</param>
    public virtual void PlayOneShot(AudioSource audioSource)
    {
        if (clip == null || audioSource == null) return;

        // PlayOneShot ไม่กระทบ clip ปัจจุบัน
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);
    }
}
