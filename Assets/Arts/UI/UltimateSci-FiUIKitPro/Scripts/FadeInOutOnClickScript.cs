using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ultimatescifiuikit.FadeInOutOnClickScript
{
    public class FadeInOutOnClickScript : MonoBehaviour
    {
        public CanvasGroup myCanvasGroup01;
        public CanvasGroup myCanvasGroup02;
        public CanvasGroup myCanvasGroup03;
        public CanvasGroup myCanvasGroup04;
        public CanvasGroup myCanvasGroup05;



        public GameObject storyMenu;
        public GameObject multiMenu;
        public GameObject settingsMenu;
        public GameObject creditsMenu;
        public GameObject quitMenu;
        public GameObject mainMenu;

        private bool fadeIn01 = false;
        private bool fadeIn02 = false;
        private bool fadeIn03 = false;
        private bool fadeIn04 = false;
        private bool fadeIn05 = false;
        private bool fadeOut01 = false;
        private bool fadeOut02 = false;
        private bool fadeOut03 = false;
        private bool fadeOut04 = false;
        private bool fadeOut05 = false;


        void Update()
        {
            if (fadeIn01)
            {

                if (myCanvasGroup01.alpha < 1)
                {
                    myCanvasGroup01.alpha += Time.deltaTime;
                    if (myCanvasGroup01.alpha >= 1)
                    {
                        fadeIn01 = false;
                    }
                }
            }

            if (fadeOut01)
            {
                if (myCanvasGroup01.alpha >= 0)
                {
                    myCanvasGroup01.alpha = myCanvasGroup01.alpha -= (Time.deltaTime / 0.50f);
                    if (myCanvasGroup01.alpha == 0)
                    {
                        fadeOut01 = false;
                        storyMenu.SetActive(false);
                    }
                }
            }

            if (fadeIn02)
            {

                if (myCanvasGroup02.alpha < 1)
                {
                    myCanvasGroup02.alpha += Time.deltaTime;
                    if (myCanvasGroup02.alpha >= 1)
                    {
                        fadeIn02 = false;
                    }
                }
            }

            if (fadeOut02)
            {
                if (myCanvasGroup02.alpha >= 0)
                {
                    myCanvasGroup02.alpha = myCanvasGroup02.alpha -= (Time.deltaTime / 0.50f);
                    if (myCanvasGroup02.alpha == 0)
                    {
                        fadeOut02 = false;
                        multiMenu.SetActive(false);
                    }
                }
            }

            if (fadeIn03)
            {

                if (myCanvasGroup03.alpha < 1)
                {
                    myCanvasGroup03.alpha = myCanvasGroup03.alpha += (Time.deltaTime / 0.50f);
                    if (myCanvasGroup03.alpha >= 1)
                    {
                        fadeIn03 = false;
                    }
                }
            }

            if (fadeOut03)
            {
                if (myCanvasGroup03.alpha >= 0)
                {
                    myCanvasGroup03.alpha = myCanvasGroup03.alpha -= (Time.deltaTime / 0.50f);
                    if (myCanvasGroup03.alpha == 0)
                    {
                        fadeOut03 = false;
                        settingsMenu.SetActive(false);

                    }
                }
            }

            if (fadeIn04)
            {

                if (myCanvasGroup04.alpha < 1)
                {
                    myCanvasGroup04.alpha = myCanvasGroup04.alpha += (Time.deltaTime / 0.50f);
                    if (myCanvasGroup04.alpha >= 1)
                    {
                        fadeIn04 = false;
                    }
                }
            }

            if (fadeOut04)
            {
                if (myCanvasGroup04.alpha >= 0)
                {
                    myCanvasGroup04.alpha = myCanvasGroup04.alpha -= (Time.deltaTime / 0.50f);
                    if (myCanvasGroup04.alpha == 0)
                    {
                        fadeOut04 = false;
                        creditsMenu.SetActive(false);
                    }
                }
            }

            if (fadeIn05)
            {

                if (myCanvasGroup05.alpha < 1)
                {
                    myCanvasGroup05.alpha = myCanvasGroup05.alpha += (Time.deltaTime / 0.50f);
                    if (myCanvasGroup05.alpha >= 1)
                    {
                        fadeIn05 = false;
                    }
                }
            }

            if (fadeOut05)
            {
                if (myCanvasGroup05.alpha >= 0)
                {
                    myCanvasGroup05.alpha = myCanvasGroup05.alpha -= (Time.deltaTime / 0.50f);
                    if (myCanvasGroup05.alpha == 0)
                    {
                        fadeOut05 = false;
                        quitMenu.SetActive(false);
                    }
                }
            }

        }
        void EnableMainMenu()
        {
            mainMenu.SetActive(true);
        }



        public void StoryFadeInCanvas()
        {
            fadeIn01 = true;
            myCanvasGroup01.alpha = 0f;
            storyMenu.SetActive(true);
        }

        public void StoryFadeOutCanvas()
        {
            fadeOut01 = true;
            Invoke("EnableMainMenu", 0.51f);
        }

        public void MultiFadeInCanvas()
        {
            fadeIn02 = true;
            myCanvasGroup02.alpha = 0f;
            multiMenu.SetActive(true);
        }

        public void MultiFadeOutCanvas()
        {
            fadeOut02 = true;
            Invoke("EnableMainMenu", 0.51f);
        }

        public void SettingsFadeInCanvas()
        {
            fadeIn03 = true;
            myCanvasGroup03.alpha = 0f;
            settingsMenu.SetActive(true);
        }

        public void SettingsFadeOutCanvas()
        {
            fadeOut03 = true;
            Invoke("EnableMainMenu", 0.51f);
        }
        public void CreditsFadeInCanvas()
        {
            fadeIn04 = true;
            myCanvasGroup04.alpha = 0f;
            creditsMenu.SetActive(true);
        }

        public void CreditsFadeOutCanvas()
        {
            fadeOut04 = true;
            Invoke("EnableMainMenu", 0.51f);
        }

        public void QuitFadeInCanvas()
        {
            fadeIn05 = true;
            myCanvasGroup05.alpha = 0f;
            quitMenu.SetActive(true);
        }

        public void QuitFadeOutCanvas()
        {
            fadeOut05 = true;
            Invoke("EnableMainMenu", 0.51f);
        }


    }
}

