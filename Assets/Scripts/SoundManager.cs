using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioClip clickSound;
    public AudioClip mainBgm;
    public AudioClip gameBgm;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AudioListener.volume = PlayerPrefs.GetFloat("Volume", 0.5f);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "Jinsoo2":
                ChangeBGM(mainBgm);
                break;

            case "Junseo":
                ChangeBGM(gameBgm);
                break;
        }
    }

    public void ChangeBGM(AudioClip clip)
    {
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
        sfxSource.PlayOneShot(clickSound);
    }
}