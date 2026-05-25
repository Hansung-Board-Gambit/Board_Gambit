using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.CreditsScript
{
    public class CreditsScript : MonoBehaviour
    {

        public GameObject mainMenu;
        public GameObject creditsMenu;
        public Ultimatescifiuikit.FadeInOutOnClickScript.FadeInOutOnClickScript fadeinoutonclickScript;
        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;
        void Start()
        {
            audioManager = Ultimatescifiuikit.AudioManager.AudioManager.Instance;
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                Invoke("EnableMainMenu", 0.45f);
                fadeinoutonclickScript.CreditsFadeOutCanvas();

            }
        }

        void EnableMainMenu()
        {
            mainMenu.SetActive(true);
        }

        public void BackButton()
        {
            fadeinoutonclickScript.CreditsFadeOutCanvas();
        }
    }
}

