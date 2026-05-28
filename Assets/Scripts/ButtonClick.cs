using UnityEngine;

public class ButtonClick : MonoBehaviour
{
    public void PlayClick()
    {
        if (SoundManager.instance != null)
            SoundManager.instance.ButtonClick();
    }
}