using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.SingleplayerMenuScript
{
    public class SingleplayerMenuScript : MonoBehaviour
    {
        public GameObject mainMenu;
        public Button chap01Panel;

        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;
        public Ultimatescifiuikit.FadeInOutOnClickScript.FadeInOutOnClickScript fadeinoutonclickScript;

        void Start()
        {
            chap01Panel.Select();
        }


        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {

                Invoke("EnableMainMenu", 0.45f);
                fadeinoutonclickScript.StoryFadeOutCanvas();

            }
        }

        void EnableMainMenu()
        {
            mainMenu.SetActive(true);
        }

        void LoadChapter01()
        {
            SceneManager.LoadScene("Level01");
        }
        public void Chapter01Button()
        {
            LoadChapter01();
        }

        public void Chapter02Button()
        {
            LoadChapter01();
        }

        public void Chapter03Button()
        {
            LoadChapter01();
        }

        public void Chapter04Button()
        {
            LoadChapter01();
        }

        public void Chapter05Button()
        {
            LoadChapter01();
        }

        public void BackButton()
        {
            fadeinoutonclickScript.StoryFadeOutCanvas();
        }
    }
}
