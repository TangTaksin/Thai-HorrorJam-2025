using UnityEngine;

public class PlayEventAudio : MonoBehaviour
{
    public void PlaySFX(AudioEvent sfxName)
    {
        SOAudioManager.Instance?.PlaySFX(sfxName);
    }
}
