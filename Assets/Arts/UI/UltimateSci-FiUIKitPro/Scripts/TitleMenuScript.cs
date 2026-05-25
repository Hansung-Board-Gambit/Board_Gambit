using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace Ultimatescifiuikit.TitleMenuScript
{
    public class TitleMenuScript : MonoBehaviour
    {

        public GameObject titleMenu;
        public Text buttontext;

        void Start()
        {
            buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        void Update()
        {
            if (Input.anyKeyDown)
            {
                SceneManager.LoadScene("MainMenu");
                buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            }

        }

        public void PressAnyButton()
        {
            SceneManager.LoadScene("MainMenu");
            buttontext.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
        }

    }
}

