using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.HoverButtonSoundScript
{
    public class HoverButtonSoundScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {

        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;

        // Start is called before the first frame update


        public void OnPointerEnter(PointerEventData eventData)
        {
            audioManager.MenuHover();

        }

        public void OnPointerExit(PointerEventData eventData)
        {

        }

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            audioManager.ButtonClick();

        }
    }
}

