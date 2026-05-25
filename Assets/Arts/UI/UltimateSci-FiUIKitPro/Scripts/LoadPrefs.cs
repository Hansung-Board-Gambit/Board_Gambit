using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ultimatescifiuikit.SettingsMenuScript;
namespace Ultimatescifiuikit.LoadPrefs
{
    public class LoadPrefs : MonoBehaviour
    {

        [Header("General Setting")]
        [SerializeField] private bool canUse = false;
        [SerializeField] private Ultimatescifiuikit.SettingsMenuScript.SettingsMenuScript settingsMenuScript;

        [Header("Brightness Setting")]
        [SerializeField] private Slider brightnessSlider = null;
        [SerializeField] private Text brightnessTextValue = null;

        [Header("Quality Level Setting")]
        [SerializeField] private Dropdown qualityDropdown;

        [Header("Fullscreen Setting")]
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Sensitivity Setting")]
        [SerializeField] private Slider controllerSenSlider = null;
        [SerializeField] private Text controllerSenTextValue = null;

        [Header("Invert Y Setting")]
        [SerializeField] private Toggle invertYToggle = null;



        private void Awake()
        {
            if (canUse)
            {
                if (PlayerPrefs.HasKey("masterQuality"))
                {
                    int localQuality = PlayerPrefs.GetInt("masterQuality");
                    qualityDropdown.value = localQuality;
                    QualitySettings.SetQualityLevel(localQuality);
                }

                if (PlayerPrefs.HasKey("masterFullscreen"))
                {
                    int localFullscreen = PlayerPrefs.GetInt("masterFullscreen");

                    if (PlayerPrefs.GetInt("masterFullscreen") == 1)
                    {
                        Screen.fullScreen = true;
                        fullscreenToggle.isOn = true;

                    }
                    else
                    {

                        Screen.fullScreen = false;
                        fullscreenToggle.isOn = false;
                    }
                }

                if (PlayerPrefs.HasKey("masterBrightness"))
                {
                    float localBrightness = PlayerPrefs.GetFloat("masterBrightness");
                    brightnessTextValue.text = localBrightness.ToString("0.0");
                    brightnessSlider.value = localBrightness;
                    //Change the brightness
                }

                if (PlayerPrefs.HasKey("masterSen"))
                {
                    float localSensitivity = PlayerPrefs.GetFloat("masterSen");
                    controllerSenTextValue.text = localSensitivity.ToString("0.0");
                    controllerSenSlider.value = localSensitivity;
                    settingsMenuScript.mainControllerSen = Mathf.RoundToInt(localSensitivity);

                }

                if (PlayerPrefs.HasKey("masterInvertY"))
                {
                    if (PlayerPrefs.GetInt("masterInvertY") == 1)
                    {
                        invertYToggle.isOn = true;
                    }
                    else
                    {
                        invertYToggle.isOn = false;
                    }

                }
            }
        }


    }
}

