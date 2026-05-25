using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.HoverMainMenuButtonScript
{
    public class HoverMainMenuButtonScript : MonoBehaviour
    {


        public Button singleButton01;
        public Button multiButton01;
        public Button settingsButton01;
        public Button creditsButton01;
        public Button quitButton01;

        public Text singlebuttonText;
        public Text multibuttonText;
        public Text settingsbuttonText;
        public Text creditsbuttonText;
        public Text quitbuttonText;

        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;

        // Start is called before the first frame update
        void Start()
        {
            singleButton01.Select();
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);

        }



        public void SingleOnPointerEnter()
        {
            singleButton01.Select();
            singleButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void SingleOnPointerExit()
        {

            singleButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);

        }

        public void SingleOnPointerClick()
        {
            singleButton01.Select();
            singleButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void MultiOnPointerEnter()
        {
            multiButton01.Select();
            multiButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void MultiOnPointerExit()
        {

            multiButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);

        }

        public void MultiOnPointerClick()
        {
            multiButton01.Select();
            multiButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void SettingsOnPointerEnter()
        {
            settingsButton01.Select();
            settingsButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void SettingsOnPointerExit()
        {
            settingsButton01.Select();
            settingsButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void SettingsOnPointerClick()
        {
            settingsButton01.Select();
            settingsButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void CreditsOnPointerEnter()
        {
            creditsButton01.Select();
            creditsButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void CreditsOnPointerExit()
        {
            creditsButton01.Select();
            creditsButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void CreditsOnPointerClick()
        {
            creditsButton01.Select();
            creditsButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void QuitOnPointerEnter()
        {
            quitButton01.Select();
            quitButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 255);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void QuitOnPointerExit()
        {
            quitButton01.Select();
            quitButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioManager.MenuHover();
        }

        public void QuitOnPointerClick()
        {
            quitButton01.Select();
            quitButton01.GetComponent<Image>().color = new Color32(52, 221, 236, 0);
            quitbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            singlebuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            multibuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            settingsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            creditsbuttonText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }


    }
}

