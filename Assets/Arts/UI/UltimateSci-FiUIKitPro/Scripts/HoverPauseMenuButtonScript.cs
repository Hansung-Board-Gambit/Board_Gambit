using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.HoverPauseMenuButtonScript
{
    public class HoverPauseMenuButtonScript : MonoBehaviour
    {


        public Button continueButton01;
        public Button mainmenuButton01;
        public Button quittodesktopButton01;

        public Text continuebuttonText;
        public Text mainmenubuttonText;
        public Text quittodesktopbuttonText;

        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;
        // Start is called before the first frame update
        void Start()
        {
            continueButton01.Select();
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);

        }

        public void ContinueOnPointerEnter()
        {
            continueButton01.Select();
            continueButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void ContinueOnPointerExit()
        {
            continueButton01.Select();
            continueButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void ContinueOnPointerClick()
        {
            continueButton01.Select();
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void MainMenuOnPointerEnter()
        {
            mainmenuButton01.Select();
            mainmenuButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void MainMenuOnPointerExit()
        {
            mainmenuButton01.Select();
            mainmenuButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void MainMenuOnPointerClick()
        {
            mainmenuButton01.Select();
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void QuitToDesktopOnPointerEnter()
        {
            quittodesktopButton01.Select();
            quittodesktopButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void QuitToDesktopOnPointerExit()
        {
            quittodesktopButton01.Select();
            quittodesktopButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void QuitToDesktopOnPointerClick()
        {
            quittodesktopButton01.Select();
            quittodesktopbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            mainmenubuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            continuebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }
    }
}

