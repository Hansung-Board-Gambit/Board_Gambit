using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Ultimatescifiuikit.FadeOnclickScript
{
    public class FadeOnclickScript : MonoBehaviour
    {
        public CanvasGroup myCanvasGroup01;
        public CanvasGroup myCanvasGroup02;
        public CanvasGroup myCanvasGroup03;

        public GameObject settingsMenu;
        public GameObject creditsMenu;
        public GameObject quitMenu;

        private bool fadeIn01 = false;
        private bool fadeIn02 = false;
        private bool fadeIn03 = false;
        private bool fadeOut01 = false;
        private bool fadeOut02 = false;
        private bool fadeOut03 = false;


        void Update()
        {
            if (fadeIn01)
            {
                myCanvasGroup01.alpha = myCanvasGroup01.alpha + (Time.deltaTime / 0.60f);
                if (myCanvasGroup01.alpha >= 1)
                {
                    myCanvasGroup01.alpha = 1;
                    fadeIn01 = false;
                }
            }

            if (fadeOut01)
            {
                myCanvasGroup01.alpha = myCanvasGroup01.alpha + (Time.deltaTime / 0.50f);
                if (myCanvasGroup01.alpha >= 1)
                {
                    myCanvasGroup01.alpha = 0;

                    fadeOut01 = false;
                }
            }

            if (fadeIn02)
            {
                myCanvasGroup02.alpha = myCanvasGroup02.alpha + (Time.deltaTime / 0.60f);
                if (myCanvasGroup02.alpha >= 1)
                {
                    myCanvasGroup02.alpha = 1;
                    fadeIn02 = false;
                }
            }

            if (fadeOut02)
            {
                myCanvasGroup02.alpha = myCanvasGroup02.alpha + (Time.deltaTime / 0.50f);
                if (myCanvasGroup02.alpha >= 1)
                {
                    myCanvasGroup02.alpha = 0;

                    fadeOut02 = false;
                }
            }

            if (fadeIn03)
            {
                myCanvasGroup03.alpha = myCanvasGroup03.alpha + (Time.deltaTime / 0.60f);
                if (myCanvasGroup03.alpha >= 1)
                {
                    myCanvasGroup03.alpha = 1;
                    fadeIn03 = false;
                }
            }

            if (fadeOut03)
            {
                myCanvasGroup03.alpha = myCanvasGroup03.alpha - (Time.deltaTime / 2.90f);
                if (myCanvasGroup03.alpha >= 0)
                {
                    myCanvasGroup03.alpha = 0;

                    fadeOut03 = false;
                }
            }

        }


        public void SettingsFadeInCanvas()
        {
            settingsMenu.SetActive(true);
            fadeIn01 = true;

            myCanvasGroup01.alpha = 0f;
        }

        public void SettingsFadeOutCanvas()
        {
            fadeOut01 = true;
            myCanvasGroup01.alpha = 0f;
            settingsMenu.SetActive(false);
        }
        public void CreditsFadeInCanvas()
        {
            creditsMenu.SetActive(true);
            fadeIn02 = true;

            myCanvasGroup02.alpha = 0f;
        }

        public void CreditsFadeOutCanvas()
        {
            fadeOut02 = true;
            myCanvasGroup02.alpha = 0f;
            creditsMenu.SetActive(false);
        }

        public void QuitFadeInCanvas()
        {
            quitMenu.SetActive(true);
            fadeIn03 = true;
            myCanvasGroup03.alpha = 0f;
        }

        public void QuitFadeOutCanvas()
        {
            fadeOut03 = true;

        }




    }
}

