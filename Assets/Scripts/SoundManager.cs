using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioSource sfxSource;
    public AudioClip clickSound;
    public Slider volumeSlider;

    bool isMuted = false;

    void Awake()
    {
        instance = this;

        isMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
        sfxSource.mute = isMuted;


        float savedVolume = PlayerPrefs.GetFloat("Volume", 0.5f);

        AudioListener.volume = savedVolume;
        volumeSlider.value = AudioListener.volume;

        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
    }

    public void ButtonClick()
    {
        if (!isMuted)
        {
            sfxSource.PlayOneShot(clickSound);
        }
    }
}