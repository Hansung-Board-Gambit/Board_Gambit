using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.HoverSettingsButtonScript
{
    public class HoverSettingsButtonScript : MonoBehaviour
    {

        public Button controlspanbotAButton;
        public Button controlspanbotRButton;
        public Button visualspanbotAButton;
        public Button visualspanbotRButton;

        public Text controlspanbotbutAText;
        public Text controlspanbotbutRText;
        public Text visualspanbotbutAText;
        public Text visualspanbotbutRText;

        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;

        void Start()
        {
            controlspanbotbutAText.GetComponent<Text>().color = new Color32(52, 221, 236, 180);
            controlspanbotbutRText.GetComponent<Text>().color = new Color32(52, 221, 236, 180);
            visualspanbotbutAText.GetComponent<Text>().color = new Color32(52, 221, 236, 180);
            visualspanbotbutRText.GetComponent<Text>().color = new Color32(52, 221, 236, 180);
        }

        public void ControlsBotButApplyOnPointerEnter()
        {
            controlspanbotbutAText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }

        public void ControlsBotButApplyOnPointerExit()
        {
            controlspanbotbutAText.GetComponent<Text>().color = new Color32(52, 221, 236, 180);
        }

        public void ControlsBotButApplyOnPointerClick()
        {
            controlspanbotbutAText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }

        public void ControlsBotButResetOnPointerEnter()
        {
            controlspanbotbutRText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }

        public void ControlsBotButResetOnPointerExit()
        {
            controlspanbotbutRText.GetComponent<Text>().color = new Color32(52, 221, 236, 180);
        }

        public void ControlsBotButResetOnPointerClick()
        {
            controlspanbotbutRText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }

        public void VisualsBotButApplyOnPointerEnter()
        {
            visualspanbotbutAText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }

        public void VisualsBotButApplyOnPointerExit()
        {
            visualspanbotbutAText.GetComponent<Text>().color = new Color32(52, 221, 236, 180);
        }

        public void VisualsBotButApplyOnPointerClick()
        {
            visualspanbotbutAText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }

        public void VisualsBotButResetOnPointerEnter()
        {
            visualspanbotbutRText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }

        public void VisualsBotButResetOnPointerExit()
        {
            visualspanbotbutRText.GetComponent<Text>().color = new Color32(52, 221, 236, 180);
        }

        public void VisualsBotButResetOnPointerClick()
        {
            visualspanbotbutRText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            audioManager.MenuHover();
        }
    }
}

