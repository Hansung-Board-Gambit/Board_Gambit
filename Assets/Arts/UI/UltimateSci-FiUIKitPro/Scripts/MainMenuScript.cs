using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Ultimatescifiuikit.AudioManager;


namespace Ultimatescifiuikit.MainMenuScript
{
    public class MainMenuScript : MonoBehaviour
    {


        public GameObject mainMenu;
        public GameObject storyMenu;
        public GameObject multiMenu;
        public GameObject settingsMenu;
        public GameObject creditsMenu;
        public GameObject quitMenu;
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
                mainMenu.SetActive(false);
                fadeinoutonclickScript.QuitFadeInCanvas();
            }
        }


        public void MMenu()
        {
            Time.timeScale = 1.0f;
            SceneManager.LoadScene("MainMenu");
        }

        public void Single()
        {
            Time.timeScale = 1.0f;
            mainMenu.SetActive(false);
            fadeinoutonclickScript.StoryFadeInCanvas();
        }

        public void Multi()
        {
            Time.timeScale = 1.0f;
            mainMenu.SetActive(false);
            fadeinoutonclickScript.MultiFadeInCanvas();
        }

        public void Settings()
        {
            Time.timeScale = 1.0f;
            mainMenu.SetActive(false);
            fadeinoutonclickScript.SettingsFadeInCanvas();
        }

        public void Credits()
        {
            Time.timeScale = 1.0f;
            mainMenu.SetActive(false);
            fadeinoutonclickScript.CreditsFadeInCanvas();
        }

        public void Quit()
        {
            Time.timeScale = 1.0f;
            mainMenu.SetActive(false);
            fadeinoutonclickScript.QuitFadeInCanvas();
        }

        public void ExitYes()
        {
            Time.timeScale = 1.0f;
            Application.Quit();
        }

        public void ExitNo()
        {
            Time.timeScale = 1.0f;
            fadeinoutonclickScript.QuitFadeOutCanvas();
        }

    }
}


