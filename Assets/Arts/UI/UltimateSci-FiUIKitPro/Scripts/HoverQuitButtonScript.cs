using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.HoverQuitButtonScript
{
    public class HoverQuitButtonScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Button quitbottomButton;

        public Image escbottomImage;

        public Text quitbottomButtonText;
        public Text escbottomButtonText;

        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;
        void Start()
        {

            quitbottomButtonText.GetComponent<Text>().color = new Color32(52, 221, 236, 150);
            escbottomButtonText.GetComponent<Text>().color = new Color32(52, 221, 236, 150);
            escbottomImage.GetComponent<Image>().color = new Color32(52, 221, 236, 150);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {

            quitbottomButtonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            escbottomButtonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            escbottomImage.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {

            quitbottomButtonText.GetComponent<Text>().color = new Color32(52, 221, 236, 150);
            escbottomButtonText.GetComponent<Text>().color = new Color32(52, 221, 236, 150);
            escbottomImage.GetComponent<Image>().color = new Color32(52, 221, 236, 150);
        }

        public void OnPointerClick(PointerEventData pointerEventData)
        {

            quitbottomButtonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            escbottomButtonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            escbottomImage.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }
    }
}

