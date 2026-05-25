using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Ultimatescifiuikit.AudioManager
{
    public class AudioManager : MonoBehaviour
    {

        public static AudioManager Instance;
        public AudioSource menuMusic;

        public AudioSource clickSound01;
        public AudioSource clickSound02;
        public AudioSource clickSound03;

        public AudioMixer theMixer;

        void Awake()
        {

            // there is one single public static instance of one class
            if (Instance == null)
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            StartMenuMusic();
            allAS = GetComponents<AudioSource>();
            if (PlayerPrefs.HasKey("MasterVol"))
            {
                theMixer.SetFloat("MasterVol", PlayerPrefs.GetFloat("MasterVol"));
            }

            if (PlayerPrefs.HasKey("MusicVol"))
            {
                theMixer.SetFloat("MusicVol", PlayerPrefs.GetFloat("MusicVol"));
            }

            if (PlayerPrefs.HasKey("SFXVol"))
            {
                theMixer.SetFloat("SFXVol", PlayerPrefs.GetFloat("SFXVol"));
            }

            AudioSource[] audios = GetComponents<AudioSource>();

            clickSound01 = audios[1];
            clickSound02 = audios[2];
            clickSound03 = audios[3];


        }

        public AudioSource[] allAS;
        public void StartMenuMusic()
        {
            menuMusic.loop = true;
            menuMusic.Play();
        }



        public void ChangeMusic(AudioClip music)
        {
            if (menuMusic.clip.name == music.name)
                return;
            menuMusic.Stop();
            menuMusic.clip = music;
            menuMusic.Play();
        }


        public void ButtonTrigger()
        {
            AudioManager.Instance.clickSound01.Play();
        }

        public void ButtonClick()
        {
            AudioManager.Instance.clickSound02.Play();
        }

        public void MenuHover()
        {
            AudioManager.Instance.clickSound03.Play();
        }

    }


}