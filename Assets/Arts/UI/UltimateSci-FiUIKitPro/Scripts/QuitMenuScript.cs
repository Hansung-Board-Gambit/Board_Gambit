using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.QuitMenuScript
{
    public class QuitMenuScript : MonoBehaviour
    {
        public GameObject mainMenu;
        public GameObject quitMenu;
        public Ultimatescifiuikit.FadeInOutOnClickScript.FadeInOutOnClickScript fadeinoutonclickScript;
        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                Invoke("EnableMainMenu", 0.45f);
                fadeinoutonclickScript.QuitFadeOutCanvas();

            }
        }
        void EnableMainMenu()
        {
            mainMenu.SetActive(true);
        }


        public void Yes()
        {
            Application.Quit();
        }

        public void No()
        {
            fadeinoutonclickScript.QuitFadeOutCanvas();
        }
    }
}
