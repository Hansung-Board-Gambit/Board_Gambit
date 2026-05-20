using Fusion;
using UnityEngine;

public class LobbyState : NetworkBehaviour
{
    public static LobbyState Instance;

    [Networked] public NetworkBool guestReady { get; set; }
    [Networked] public int gameValue { get; set; }
    [Networked] public NetworkString<_16> hostName { get; set; }
    [Networked] public NetworkString<_16> guestName { get; set; }

    void OnGuestReadyUpdated()
    {
        Debug.Log("Guest Ready 상태 변경됨: " + guestReady);

        NetworkManager manager = FindObjectOfType<NetworkManager>();
        if (manager != null)
        {
            manager.UpdateGuestUI(guestReady);
        }
    }

    public override void Spawned()
    {
        Debug.Log("LobbyState Spawned");

        Instance = this;

        if (Object.HasStateAuthority)
        {
            gameValue = 2; 
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetHostName(string name)
    {
        if (Object.HasStateAuthority)
        {
            hostName = name;
        }
    }

    public void SetGuestName(string name)
    {
        if (Runner.IsClient)
        {
            RPC_SetGuestName(name);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SetGuestName(string name)
    {
        guestName = name;
    }

    public void ToggleGuestReady()
    {
        if (Runner.IsClient)
        {
            RPC_SetGuestReady();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SetGuestReady()
    {
        Debug.Log("RPC 실행됨 (Host)");

        guestReady = !guestReady;
    }

    public void ResetGuestReady()
    {
        if (Object.HasStateAuthority)
        {
            guestReady = false;
        }
    }

    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    //void RPC_StartGame()
    //{
    //    Debug.Log("모든 클라이언트 게임 시작");

    //    NetworkManager manager = FindObjectOfType<NetworkManager>();

    //    manager.InitCanvas.SetActive(false);
    //    manager.LobbyCanvas.SetActive(false);
    //    manager.HostUI.SetActive(false);
    //    manager.GuestUI.SetActive(false);
    //}

    public void StartGame()
    {
        if (Object.HasStateAuthority)
        {
            Runner.LoadScene("Junseo");
        }
    }
}