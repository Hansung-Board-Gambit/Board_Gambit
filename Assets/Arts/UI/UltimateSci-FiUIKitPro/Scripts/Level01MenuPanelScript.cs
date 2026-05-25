using System.Collections;
using System.Collections.Generic;
using Ultimatescifiuikit.FadeInOutOnClickScript;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Level01MenuPanelScript.Level01MenuPanelScript
{
    public class Level01MenuPanelScript : MonoBehaviour
    {
        public GameObject GMenu;
        public GameObject VMenu;

        // Start is called before the first frame update
        void Start()
        {
            GMenu.SetActive(false);
            VMenu.SetActive(false);
        }

        void Update()
        {

            if (Input.GetKeyUp(KeyCode.W))
            {
                Time.timeScale = 0.0f;
                VMenu.SetActive(true);
            }

            if (Input.GetKeyUp(KeyCode.G))
            {
                Time.timeScale = 0.0f;
                GMenu.SetActive(true);
            }
        }

        void LoadMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void VictoryMMainMenuButton()
        {
            Time.timeScale = 1.0f;
            VMenu.SetActive(false);
            LoadMenu();
        }
        public void VictoryMContinueButton()
        {
            Time.timeScale = 1.0f;
            VMenu.SetActive(false);

        }

        public void GameoverMYesButton()
        {
            Time.timeScale = 1.0f;
            GMenu.SetActive(false);
            LoadMenu();
        }

        public void GameoverMNoButton()
        {
            Time.timeScale = 1.0f;
            GMenu.SetActive(false);

        }
    }
}

