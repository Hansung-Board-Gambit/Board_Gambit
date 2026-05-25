using UnityEngine;
using System.Collections;
using Ultimatescifiuikit.AudioManager;
namespace Ultimatescifiuikit.SwitchMusicOnLoad
{
    public class SwitchMusicOnLoad : MonoBehaviour
    {

        public AudioClip musiclvl;
        public AudioSource clickSound01;
        public AudioSource clickSound02;
        public AudioSource clickSound03;
        private Ultimatescifiuikit.AudioManager.AudioManager theSM;

        void Start()
        {
            //theSM = FindObjectOfType<AudioManager>();
            theSM = Ultimatescifiuikit.AudioManager.AudioManager.Instance;


            if (musiclvl != null)
                theSM.ChangeMusic(musiclvl);

        }



        public void ButtonTrigger()
        {
            Ultimatescifiuikit.AudioManager.AudioManager.Instance.clickSound01.Play();
        }

        public void ButtonClick()
        {
            Ultimatescifiuikit.AudioManager.AudioManager.Instance.clickSound02.Play();
        }

        public void MenuHover()
        {
            Ultimatescifiuikit.AudioManager.AudioManager.Instance.clickSound03.Play();
        }
    }

}