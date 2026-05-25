using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.MultiplayerMenuScript
{
    public class MultiplayerMenuScript : MonoBehaviour
    {
        public GameObject mainMenu;
        public Button deathmatchPanel;
        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;
        public GameObject searchOverlay;
        public Ultimatescifiuikit.FadeInOutOnClickScript.FadeInOutOnClickScript fadeinoutonclickScript;

        void Start()
        {
            searchOverlay.SetActive(false);
            deathmatchPanel.Select();
        }


        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                Invoke("EnableMainMenu", 0.45f);
                fadeinoutonclickScript.MultiFadeOutCanvas();
                searchOverlay.SetActive(false);
            }
        }

        void EnableMainMenu()
        {
            mainMenu.SetActive(true);
        }

        public void DeathmatchButton()
        {
            searchOverlay.SetActive(true);
        }

        public void SearchandDestroyButton()
        {
            searchOverlay.SetActive(true);
        }

        public void DominationButton()
        {
            searchOverlay.SetActive(true);
        }

        public void CaptureTheFlagButton()
        {
            searchOverlay.SetActive(true);
        }

        public void TrainingButton()
        {
            searchOverlay.SetActive(true);
        }

        public void CancelButton()
        {
            searchOverlay.SetActive(false);
        }

        public void BackButton()
        {
            fadeinoutonclickScript.MultiFadeOutCanvas();
        }
    }
}

