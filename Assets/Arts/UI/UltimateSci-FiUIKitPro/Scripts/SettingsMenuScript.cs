using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Ultimatescifiuikit.AudioManager;
using Ultimatescifiuikit.LoadPrefs;

namespace Ultimatescifiuikit.SettingsMenuScript
{
    public class SettingsMenuScript : MonoBehaviour
    {
        public Toggle enabletut;
        public Toggle devconsole;
        public Toggle vSyncToggle;



        public GameObject mainMenu;
        public GameObject settingsMenu;
        public GameObject generalPanel;
        public GameObject controlsPanel;
        public GameObject visualsPanel;
        public GameObject audioPanel;
        public GameObject controlsbuttonPanel;
        public GameObject visualsbuttonPanel;
        public GameObject generalButton01;


        public Button generalButton;
        public Button controlsButton;
        public Button visualsButton;
        public Button audioButton;



        public Text generalText;
        public Text controlsText;
        public Text visualsText;
        public Text audioText;



        public Text mastLabel, musicLabel, sfxLabel;

        public AudioMixer theMixer;
        public Slider mastSlider, musicSlider, sfxSlider;
        public Ultimatescifiuikit.FadeInOutOnClickScript.FadeInOutOnClickScript fadeinoutonclickScript;
        //  public AudioManager audioManager;
        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;

        private int selectedQuality;
        [Header("Toggle Settings")]
        [SerializeField] private Toggle invertYToggle = null;

        [Header("Resolution Dropdowns")]
        public Dropdown resolutionDropdown;
        private Resolution[] resolutions;

        [Header("Graphics Settings")]
        [SerializeField] private Slider brightnessSlider = null;
        [SerializeField] private Text brightnessTextValue = null;
        [SerializeField] private float defaultBrightness = 1;

        [Space(10)]
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Controls Settings")]
        [SerializeField] private Slider controllerSenSlider = null;
        [SerializeField] private Text controllerSenTextValue = null;
        [SerializeField] private int defaultSen = 4;
        public int mainControllerSen = 4;

        private int _qualityLevel;
        private bool _isFullscreen;
        private float _brightnessLevel;



        void Start()
        {
            //            DeleteAll(); 

            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + "x" + resolutions[i].height + " " + resolutions[i].refreshRate.ToString() + "Hz";
                options.Add(option);

                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = i;
                }
            }

            if (QualitySettings.vSyncCount == 0)
            {
                vSyncToggle.isOn = false;
            }

            else
            {
                vSyncToggle.isOn = true;
            }



            float vol = 0f;
            theMixer.GetFloat("MasterVol", out vol);
            mastSlider.value = vol;
            theMixer.GetFloat("MusicVol", out vol);
            musicSlider.value = vol;
            theMixer.GetFloat("SFXVol", out vol);
            sfxSlider.value = vol;

            mastLabel.text = Mathf.RoundToInt(mastSlider.value + 80).ToString();
            musicLabel.text = Mathf.RoundToInt(musicSlider.value + 80).ToString();
            sfxLabel.text = Mathf.RoundToInt(sfxSlider.value + 80).ToString();



            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();


            generalButton.Select();
            controlsText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            visualsText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);


        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                Invoke("EnableMainMenu", 0.45f);
                fadeinoutonclickScript.SettingsFadeOutCanvas();

            }

        }

        void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }

        void EnableMainMenu()
        {
            mainMenu.SetActive(true);
        }

        public void SetMasterVol()
        {
            mastLabel.text = Mathf.RoundToInt(mastSlider.value + 80).ToString();
            theMixer.SetFloat("MasterVol", mastSlider.value);
            PlayerPrefs.SetFloat("MasterVol", mastSlider.value);
        }

        public void SetMusicVol()
        {
            musicLabel.text = Mathf.RoundToInt(musicSlider.value + 80).ToString();
            theMixer.SetFloat("MusicVol", musicSlider.value);
            PlayerPrefs.SetFloat("MusicVol", musicSlider.value);
        }

        public void SetSFXVol()
        {
            sfxLabel.text = Mathf.RoundToInt(sfxSlider.value + 80).ToString();
            theMixer.SetFloat("SFXVol", sfxSlider.value);
            PlayerPrefs.SetFloat("SFXVol", sfxSlider.value);
        }
        public void SetResolution(int resolutionIndex)
        {
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            audioManager.ButtonClick();
        }

        public void SetBrightness(float brightness)
        {
            _brightnessLevel = brightness;
            brightnessTextValue.text = brightness.ToString("0.0");
        }

        public void SetFullscreen(bool isFullscreen)
        {
            _isFullscreen = isFullscreen;
        }

        public void SetQuality(int qualityIndex)
        {
            _qualityLevel = qualityIndex;
        }

        public void SetControllerSen(float sensitivity)
        {
            mainControllerSen = Mathf.RoundToInt(sensitivity);
            controllerSenTextValue.text = sensitivity.ToString("0");
        }

        public void ControlsApply()
        {
            if (invertYToggle.isOn)
            {
                PlayerPrefs.SetInt("masterInvertY", 1);
                //invert Y
            }
            else
            {
                PlayerPrefs.SetInt("masterInvertY", 0);
                //not invert
            }

            PlayerPrefs.SetFloat("masterSen", mainControllerSen);
        }

        public void ApplyVSync()
        {
            if (vSyncToggle.isOn)
            {
                QualitySettings.vSyncCount = 1;
            }
            else
            {
                QualitySettings.vSyncCount = 0;
            }

        }

        public void GraphicsApply()
        {
            PlayerPrefs.SetFloat("masterBrightness", _brightnessLevel);
            PlayerPrefs.SetInt("masterQuality", _qualityLevel);
            QualitySettings.SetQualityLevel(_qualityLevel);

            PlayerPrefs.SetInt("masterFullscreen", (_isFullscreen ? 1 : 0));
            Screen.fullScreen = _isFullscreen;
        }

        public void ResetToDefaultButton(string MenuType)
        {

            if (MenuType == "GraphicsPanel")
            {
                //Reset brightness value
                brightnessSlider.value = defaultBrightness;
                brightnessTextValue.text = defaultBrightness.ToString("0.0");

                qualityDropdown.value = 1;
                QualitySettings.SetQualityLevel(1);

                //Reset fullscreen
                fullscreenToggle.isOn = false;
                Screen.fullScreen = false;

                Resolution currentResolution = Screen.currentResolution;
                Screen.SetResolution(currentResolution.width, currentResolution.height, Screen.fullScreen);
                resolutionDropdown.value = resolutions.Length;
                GraphicsApply();
            }

            if (MenuType == "ControlsPanel")
            {
                //Reset controls value
                controllerSenTextValue.text = defaultSen.ToString("0");
                controllerSenSlider.value = defaultSen;
                mainControllerSen = defaultSen;
                invertYToggle.isOn = false;
                ControlsApply();
            }
        }


        public void GeneralTopButton()
        {
            generalPanel.SetActive(true);
            controlsPanel.SetActive(false);
            visualsPanel.SetActive(false);
            audioPanel.SetActive(false);
            controlsbuttonPanel.SetActive(false);
            visualsbuttonPanel.SetActive(false);
            generalButton.Select();
            generalText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            controlsText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            visualsText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void ControlsTopButton()
        {
            controlsPanel.SetActive(true);
            generalPanel.SetActive(false);
            visualsPanel.SetActive(false);
            audioPanel.SetActive(false);
            controlsbuttonPanel.SetActive(true);
            visualsbuttonPanel.SetActive(false);
            controlsButton.Select();
            controlsText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            generalText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            visualsText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void VisualsTopButton()
        {
            visualsPanel.SetActive(true);
            controlsPanel.SetActive(false);
            generalPanel.SetActive(false);
            audioPanel.SetActive(false);
            controlsbuttonPanel.SetActive(false);
            visualsbuttonPanel.SetActive(true);
            visualsButton.Select();
            visualsText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            controlsText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            generalText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            audioText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
        }

        public void AudioTopButton()
        {
            audioPanel.SetActive(true);
            controlsPanel.SetActive(false);
            generalPanel.SetActive(false);
            visualsPanel.SetActive(false);
            audioButton.Select();
            controlsbuttonPanel.SetActive(false);
            visualsbuttonPanel.SetActive(false);
            audioText.GetComponent<Text>().color = new Color32(52, 221, 236, 255);
            controlsText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            generalText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);
            visualsText.GetComponent<Text>().color = new Color32(52, 221, 236, 80);

        }


        public void BackButton()
        {
            fadeinoutonclickScript.SettingsFadeOutCanvas();
        }


    }



    [System.Serializable]
    public class ResItem
    {
        public int horizontal, vertical;
    }



}
