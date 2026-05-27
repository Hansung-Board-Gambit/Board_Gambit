using UnityEngine;
using UnityEngine.UI;

public class SoundSlider : MonoBehaviour
{
    public Slider volumeSlider;

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("Volume", 0.5f);

        volumeSlider.SetValueWithoutNotify(savedVolume);
        volumeSlider.onValueChanged.RemoveAllListeners();
        volumeSlider.onValueChanged.AddListener(SoundManager.instance.SetVolume);
    }
}