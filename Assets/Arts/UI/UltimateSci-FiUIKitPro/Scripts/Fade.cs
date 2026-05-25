using UnityEngine;
using System.Collections;

namespace Ultimatescifiuikit.Fade
{
    public class Fade : MonoBehaviour
    {


        public CanvasGroup myCanvasGroup;


        private bool fadeIn;


        void Start()
        {
            fadeIn = true;

            myCanvasGroup.alpha = 0f;

        }

        void Update()
        {
            if (fadeIn)
            {
                myCanvasGroup.alpha = myCanvasGroup.alpha + (Time.deltaTime / 0.65f);
                if (myCanvasGroup.alpha >= 1)
                {
                    myCanvasGroup.alpha = 1;
                    fadeIn = false;
                }
            }


        }
    }
}
