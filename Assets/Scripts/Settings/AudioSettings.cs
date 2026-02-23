using UnityEngine;
using UnityEngine.Audio;

public class AudioSettings : MonoBehaviour
{
    public AudioMixer audioMixer;

    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", value);
    }
    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", value);
    }
    public void SetEffectsVolume(float value)
    {
        audioMixer.SetFloat("EffectsVolume ", value);
    }
}
