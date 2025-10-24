using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#region Helper Data Structures
public enum SurfaceType
{
    Wood,
    Concrete,
    Grass,
    Metal,
    Carpet,
    Gravel,
    Water,
    Sand
}

[System.Serializable]
public class SurfaceSound
{
    public SurfaceType surface;
    public AudioClip[] clips;
}
#endregion

[CreateAssetMenu(menuName = "Audio/Footstep Audio Event")]
public class FootstepAudioEvent : AudioEvent
{
    [Header("Footstep Settings")]
    [Tooltip("รายการเสียงฝีเท้าที่แบ่งตามประเภทของพื้นผิว")]
    public SurfaceSound[] surfaceSounds;

    [Header("Cooldown Settings")]
    [Range(0.1f, 2f)] public float minStepDelay = 0.2f;
    [Range(0.1f, 2f)] public float maxStepDelay = 0.4f;

    [Header("Randomization")]
    [Range(0f, 0.3f)] public float pitchVariation = 0.05f;
    [Range(0f, 0.3f)] public float volumeVariation = 0.05f;

    // Per-AudioSource cooldown tracking
    private Dictionary<AudioSource, CooldownData> cooldownTracker = new Dictionary<AudioSource, CooldownData>();

    private struct CooldownData
    {
        public float lastPlayTime;
        public float nextStepDelay;
    }

    public virtual void PlayOneShot(AudioSource audioSource, SurfaceType surface)
    {
        if (audioSource == null) return;

        if (!CanPlay(audioSource)) return;

        AudioClip clip = GetRandomClip(surface);
        if (clip == null) return;

        PlayClip(audioSource, clip);
        UpdateCooldown(audioSource);
    }

    private bool CanPlay(AudioSource audioSource)
    {
        if (!cooldownTracker.ContainsKey(audioSource))
        {
            cooldownTracker[audioSource] = new CooldownData();
            return true;
        }

        var data = cooldownTracker[audioSource];
        return Time.time - data.lastPlayTime >= data.nextStepDelay;
    }

    private AudioClip GetRandomClip(SurfaceType surface)
    {
        var sound = surfaceSounds.FirstOrDefault(s => s.surface == surface);
        
        if (sound == null || sound.clips == null || sound.clips.Length == 0)
        {
            Debug.LogWarning($"ไม่พบเสียงสำหรับพื้นผิว: {surface} ใน {name}");
            return null;
        }

        return sound.clips[Random.Range(0, sound.clips.Length)];
    }

    private void PlayClip(AudioSource audioSource, AudioClip clip)
    {
        audioSource.pitch = pitch + Random.Range(-pitchVariation, pitchVariation);
        audioSource.volume = volume + Random.Range(-volumeVariation, volumeVariation);
        audioSource.PlayOneShot(clip);
    }

    private void UpdateCooldown(AudioSource audioSource)
    {
        cooldownTracker[audioSource] = new CooldownData
        {
            lastPlayTime = Time.time,
            nextStepDelay = Random.Range(minStepDelay, maxStepDelay)
        };
    }

    public void ResetCooldown(AudioSource audioSource = null)
    {
        if (audioSource == null)
        {
            cooldownTracker.Clear();
        }
        else if (cooldownTracker.ContainsKey(audioSource))
        {
            cooldownTracker.Remove(audioSource);
        }
    }

    private void OnDisable()
    {
        CleanupNullReferences();
    }

    private void CleanupNullReferences()
    {
        var nullKeys = cooldownTracker.Keys.Where(k => k == null).ToList();
        foreach (var key in nullKeys)
        {
            cooldownTracker.Remove(key);
        }
    }

    #region Prevent Incorrect Usage
    public override void Play(AudioSource audioSource)
    {
        Debug.LogError($"'{name}' requires a SurfaceType. Use PlayOneShot(audioSource, surfaceType) instead.");
    }

    public override void PlayOneShot(AudioSource audioSource)
    {
        Debug.LogError($"'{name}' requires a SurfaceType. Use PlayOneShot(audioSource, surfaceType) instead.");
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// ตรวจจับประเภทพื้นผิวจาก RaycastHit ผ่าน tag
    /// </summary>
    public static SurfaceType DetectSurfaceFromTag(string tag)
    {
        switch (tag)
        {
            case "Wood": return SurfaceType.Wood;
            case "Concrete": return SurfaceType.Concrete;
            case "Grass": return SurfaceType.Grass;
            case "Metal": return SurfaceType.Metal;
            case "Carpet": return SurfaceType.Carpet;
            case "Gravel": return SurfaceType.Gravel;
            case "Water": return SurfaceType.Water;
            case "Sand": return SurfaceType.Sand;
            default: return SurfaceType.Concrete;
        }
    }

    /// <summary>
    /// ตรวจสอบว่ามีเสียงสำหรับพื้นผิวนี้หรือไม่
    /// </summary>
    public bool HasSoundForSurface(SurfaceType surface)
    {
        var sound = surfaceSounds.FirstOrDefault(s => s.surface == surface);
        return sound != null && sound.clips != null && sound.clips.Length > 0;
    }
    #endregion
}