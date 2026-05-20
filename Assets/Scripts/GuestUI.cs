using UnityEngine;

public class GuestUI : MonoBehaviour
{
    public LobbyState lobby;

    public void ReadyButton()
    {
        lobby.ToggleGuestReady();
        Debug.Log("Guest Ready");
    }
}