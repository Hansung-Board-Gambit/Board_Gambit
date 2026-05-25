using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Ultimatescifiuikit.AudioManager;
using UnityEditor;

namespace Level01MenuScript.Level01MenuScript
{
    public class Level01MenuScript : MonoBehaviour
    {
        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;
        public GameObject lvl01menuText;
        public GameObject pressmenuButton;
        public GameObject lvl01menuPanel;
        public GameObject chapmenuBG;
       
        public Text buttontext;


        void Start()
        {
            lvl01menuText.SetActive(true);
            pressmenuButton.SetActive(true);
            lvl01menuPanel.SetActive(false);
            chapmenuBG.SetActive(true);
            buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                Time.timeScale = 1.0f;
                LoadStart();
                buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 255);

            }

        }

        void LoadMMenu()
        {
            SceneManager.LoadScene("MainMenu");

        }

        // For Start Button
        void LoadStart()
        {
            lvl01menuText.SetActive(false);
            pressmenuButton.SetActive(false);
            chapmenuBG.SetActive(false);
            lvl01menuPanel.SetActive(true);
        }

        public void PressStartButton()
        {
            LoadStart();
            buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
        }
        public void BackButton()
        {
            LoadMMenu();
        }
    }
}

