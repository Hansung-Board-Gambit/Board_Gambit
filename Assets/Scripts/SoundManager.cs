using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioClip clickSound;
    public AudioClip clickSound2;
    public AudioClip mainBgm;
    public AudioClip gameBgm;

    void Awake()
    {
        instance = this;

        AudioListener.volume = PlayerPrefs.GetFloat("Volume", 0.5f);

        ChangeBGM(mainBgm);
    }

    public void ChangeBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null)
            return;

        if (bgmSource.clip == clip)
            return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
        PlayerPrefs.Save();
    }

    public void ButtonClick()
    {
        if (sfxSource == null || clickSound == null)
            return;

        sfxSource.PlayOneShot(clickSound);
    }

    public void ButtonClick2()
    {
        if (sfxSource == null || clickSound == null)
            return;

        sfxSource.PlayOneShot(clickSound2);
    }
}