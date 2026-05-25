using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.HoverButtonTitleMenuScript
{
    public class HoverButtonTitleMenuScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public RectTransform Button;
        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;
        public Text buttontext;

        void Start()
        {
            Button.GetComponent<Animator>().Play("PressAnyKeyButtonOnIdleAnim");
            buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Button.GetComponent<Animator>().Play("PressAnyKeyButtonOnHoverAnim");
            audioManager.MenuHover();
            buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Button.GetComponent<Animator>().Play("PressAnyKeyButtonOffHoverAnim");
            buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            Button.GetComponent<Animator>().Play("PressAnyKeyButtonOnHoverAnim");
            audioManager.ButtonTrigger();
            buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
        }
    }
}


