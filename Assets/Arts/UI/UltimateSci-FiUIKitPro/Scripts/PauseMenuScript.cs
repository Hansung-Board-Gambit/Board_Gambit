using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.PauseMenuScript
{
    public class PauseMenuScript : MonoBehaviour
    {

        public bool GameIsPaused = false;
        public GameObject pauseMenu;
        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;


        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (GameIsPaused)
                {
                    Resume();
                }

                else
                {
                    OnPause();
                }

            }
        }

        void LoadRestart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void LoadMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        void LoadLevelSelect()
        {
            SceneManager.LoadScene("Multiplayer");
        }

        void EnableTimeStop()
        {
            Time.timeScale = 0.0f;
        }

        void DisableMenu()
        {
            pauseMenu.SetActive(false);
        }

        public void OnPause()
        {
            pauseMenu.SetActive(true);
            GameIsPaused = true;
            Invoke("EnableTimeStop", 0.52f);
        }

        public void Resume()
        {
            Time.timeScale = 1.0f;
            GameIsPaused = false;
            Invoke("DisableMenu", 0.5f);
        }

        public void LevelSelect()
        {
            Time.timeScale = 1.0f;
            LoadLevelSelect();
            pauseMenu.SetActive(false);
        }

        public void MainMenu()
        {
            Time.timeScale = 1.0f;
            LoadMenu();
            pauseMenu.SetActive(false);
        }

        public void Restart()
        {
            Time.timeScale = 1.0f;
            pauseMenu.SetActive(false);
            LoadRestart();
        }

        public void ExitButton()
        {
            Application.Quit();
        }





    }
}

