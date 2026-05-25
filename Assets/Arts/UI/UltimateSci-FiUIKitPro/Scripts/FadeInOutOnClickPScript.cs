using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ultimatescifiuikit.FadeInOutOnClickPScript
{
    public class FadeInOutOnClickPScript : MonoBehaviour
    {
        public CanvasGroup myCanvasGroup01;
        public CanvasGroup myCanvasGroup02;
        public GameObject firstMenu;
        public GameObject secondMenu;

        private bool fadeIn01 = false;
        private bool fadeIn02 = false;

        private bool fadeOut01 = false;
        private bool fadeOut02 = false;



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
                        //                  firstMenu.SetActive(false);
                    }
                }
            }

            if (fadeIn02)
            {

                if (myCanvasGroup02.alpha < 1)
                {
                    myCanvasGroup02.alpha = myCanvasGroup02.alpha += (Time.deltaTime / 0.50f);
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
                        //                  secondMenu.SetActive(false);
                    }
                }
            }

        }
        void EnableFirstMenu()
        {
            firstMenu.SetActive(true);
        }


        public void FirstFadeInCanvas()
        {
            fadeIn01 = true;
            myCanvasGroup01.alpha = 0f;
            firstMenu.SetActive(true);
        }

        public void FirstFadeOutCanvas()
        {
            fadeOut01 = true;
            Invoke("EnableFirstMenu", 0.45f);
        }
        public void SecondFadeInCanvas()
        {
            fadeIn02 = true;
            myCanvasGroup02.alpha = 0f;
            //      secondMenu.SetActive(true);
        }

        public void SecondFadeOutCanvas()
        {
            fadeOut02 = true;
            float scale = 0f;
            scale += Time.unscaledDeltaTime;
            //      Invoke("EnableFirstMenu", 0.45f);
        }
    }
}


