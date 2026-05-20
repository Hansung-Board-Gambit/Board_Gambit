using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManger : MonoBehaviour
{

    public GameObject WarningPanel;
    public GameObject JoinPanel;
    public GameObject wrongCode;
    public CanvasGroup SharingUI;
    public CanvasGroup HostUI;
    public CanvasGroup GuestUI;
    public TMP_InputField roomInput;
    public GameObject InitCanvas;  //ÃÊ±â ¾À Äµ¹ö½º
    public GameObject OptionCanvas;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OpenExitWarning()
    {
        WarningPanel.SetActive(true);
        SharingUI.interactable = false;
        SharingUI.blocksRaycasts = false;
        HostUI.interactable = false;
        HostUI.blocksRaycasts = false;
        GuestUI.interactable = false;
        GuestUI.blocksRaycasts = false;
    }

    public void CloseExitWarning()
    {
        WarningPanel.SetActive(false);
        SharingUI.interactable = true;
        SharingUI.blocksRaycasts = true;
        HostUI.interactable = true;
        HostUI.blocksRaycasts = true;
        GuestUI.interactable = true;
        GuestUI.blocksRaycasts = true;
    }
    public void OpenJoinPanel()
    {
        JoinPanel.SetActive(true);
        roomInput.text = "";
        roomInput.ActivateInputField();
    }

    public void CloseJoinPanel()
    {
        JoinPanel.SetActive(false);
        wrongCode.SetActive(false);
        roomInput.text = "";
    }

    public void MoveOption()
    {
        InitCanvas.SetActive(false);
        OptionCanvas.SetActive(true);
    }

    public void QuitOption()
    {
        OptionCanvas.SetActive(false);
        InitCanvas.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit");
        Application.Quit();
    }
}
