using Fusion;
using System;
using UnityEngine;

public class LobbyState : NetworkBehaviour
{
    public static LobbyState Instance;
    public static event Action<int> PrepPhaseSkipRequested;
    public static event Action<int, Vector3, Quaternion> PrepObjectPlaced;
    public static event Action<bool, Vector3> PrepSpawnPlaced;
    public static event Action PrepEquipmentAllReady;
    public static event Action BattlePlayerSpawnRequested;
    public static event Action<int, int, int> RoundResultAnnounced;

    [Networked] public NetworkBool guestReady { get; set; }
    [Networked] public int gameValue { get; set; }
    [Networked] public NetworkString<_16> hostName { get; set; }
    [Networked] public NetworkString<_16> guestName { get; set; }
    [Networked] public int prepRound { get; set; }
    [Networked] public NetworkBool objectPlacementAuthorityIsHost { get; set; }
    [Networked] public NetworkBool hostEquipmentReady { get; set; }
    [Networked] public NetworkBool guestEquipmentReady { get; set; }
    [Networked] public int hostSelectedEquipmentIndex { get; set; }
    [Networked] public int guestSelectedEquipmentIndex { get; set; }
    [Networked] public int hostRoundScore { get; set; }
    [Networked] public int guestRoundScore { get; set; }
    [Networked] public int prepPhaseIndex { get; set; }
    [Networked] public TickTimer prepPhaseTimer { get; set; }

    void OnGuestReadyUpdated()
    {
        Debug.Log("Guest Ready state changed: " + guestReady);

        NetworkManager manager = FindFirstObjectByType<NetworkManager>();
        if (manager != null)
            manager.UpdateGuestUI(guestReady);
    }

    public override void Spawned()
    {
        Debug.Log("LobbyState Spawned");
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (Object.HasStateAuthority)
        {
            gameValue = 2;
            prepRound = 1;
            objectPlacementAuthorityIsHost = UnityEngine.Random.value >= 0.5f;
            hostSelectedEquipmentIndex = -1;
            guestSelectedEquipmentIndex = -1;
            hostRoundScore = 0;
            guestRoundScore = 0;
            prepPhaseIndex = -1;
            prepPhaseTimer = TickTimer.None;
            ResetEquipmentReadyState();
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
            hostName = name;
    }

    public void SetGuestName(string name)
    {
        if (Runner.IsClient)
            RPC_SetGuestName(name);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SetGuestName(string name)
    {
        guestName = name;
    }

    public void ToggleGuestReady()
    {
        if (Runner.IsClient)
            RPC_SetGuestReady();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SetGuestReady()
    {
        Debug.Log("RPC SetGuestReady executed on host");
        guestReady = !guestReady;
    }

    public void ResetGuestReady()
    {
        if (Object.HasStateAuthority)
            guestReady = false;
    }

    public void StartGame()
    {
        if (Object.HasStateAuthority)
            Runner.LoadScene("Junseo");
    }

    public bool LocalHasObjectPlacementAuthority()
    {
        if (Runner == null)
            return false;

        return Runner.IsServer == objectPlacementAuthorityIsHost;
    }

    public bool LocalHasSpawnPlacementAuthority()
    {
        if (Runner == null)
            return false;

        return Runner.IsServer != objectPlacementAuthorityIsHost;
    }

    public string GetLocalAuthorityDebugText()
    {
        string objectOwner = objectPlacementAuthorityIsHost ? "Host" : "Guest";
        string spawnOwner = objectPlacementAuthorityIsHost ? "Guest" : "Host";
        string localSide = Runner != null && Runner.IsServer ? "Host" : "Guest";
        return "Local=" + localSide + ", Object=" + objectOwner + ", Spawn=" + spawnOwner;
    }

    public void AdvancePrepRound()
    {
        if (!Object.HasStateAuthority)
            return;

        prepRound++;
        objectPlacementAuthorityIsHost = !objectPlacementAuthorityIsHost;
        ResetEquipmentReadyState();
    }

    public void ResetEquipmentReadyState()
    {
        if (!Object.HasStateAuthority)
            return;

        hostEquipmentReady = false;
        guestEquipmentReady = false;
    }

    public int GetSelectedEquipmentIndex(PlayerRef player)
    {
        return Runner != null && player == Runner.LocalPlayer ? hostSelectedEquipmentIndex : guestSelectedEquipmentIndex;
    }

    public void RequestSelectEquipment(int equipmentIndex)
    {
        if (Runner == null)
            return;

        if (Object.HasStateAuthority)
            SetSelectedEquipment(Runner.IsServer, equipmentIndex);
        else
            RPC_RequestSelectEquipment(equipmentIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSelectEquipment(int equipmentIndex)
    {
        SetSelectedEquipment(false, equipmentIndex);
    }

    private void SetSelectedEquipment(bool isHost, int equipmentIndex)
    {
        if (isHost)
            hostSelectedEquipmentIndex = equipmentIndex;
        else
            guestSelectedEquipmentIndex = equipmentIndex;

        Debug.Log("Equipment selected. IsHost=" + isHost + ", Index=" + equipmentIndex);
    }

    public void RequestSkipPrepPhase(int phaseIndex)
    {
        if (Runner == null)
            return;

        if (Object.HasStateAuthority)
            RPC_BroadcastSkipPrepPhase(phaseIndex);
        else
            RPC_RequestSkipPrepPhase(phaseIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSkipPrepPhase(int phaseIndex)
    {
        RPC_BroadcastSkipPrepPhase(phaseIndex);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastSkipPrepPhase(int phaseIndex)
    {
        PrepPhaseSkipRequested?.Invoke(phaseIndex);
    }

    public void StartPrepPhaseTimer(int phaseIndex, float duration)
    {
        if (!Object.HasStateAuthority || Runner == null)
            return;

        prepPhaseIndex = phaseIndex;
        prepPhaseTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.1f, duration));
    }

    public bool IsPrepPhaseTimerReady(int phaseIndex)
    {
        return Runner != null && prepPhaseIndex == phaseIndex && prepPhaseTimer.IsRunning;
    }

    public bool IsPrepPhaseTimerExpired(int phaseIndex)
    {
        return IsPrepPhaseTimerReady(phaseIndex) && prepPhaseTimer.Expired(Runner);
    }

    public float GetPrepPhaseTimerRatio(int phaseIndex, float duration)
    {
        if (!IsPrepPhaseTimerReady(phaseIndex))
            return 1f;

        float remaining = prepPhaseTimer.RemainingTime(Runner) ?? 0f;
        return Mathf.Clamp01(remaining / Mathf.Max(0.1f, duration));
    }

    public void RequestPlacePrepObject(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        if (Runner == null)
            return;

        if (Object.HasStateAuthority)
            RPC_BroadcastPlacePrepObject(prefabIndex, position, rotation);
        else
            RPC_RequestPlacePrepObject(prefabIndex, position, rotation);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPlacePrepObject(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        RPC_BroadcastPlacePrepObject(prefabIndex, position, rotation);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastPlacePrepObject(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        PrepObjectPlaced?.Invoke(prefabIndex, position, rotation);
    }

    public void RequestPlacePrepSpawn(bool isMySpawn, Vector3 position)
    {
        if (Runner == null)
            return;

        if (Object.HasStateAuthority)
            RPC_BroadcastPlacePrepSpawn(isMySpawn, position);
        else
            RPC_RequestPlacePrepSpawn(isMySpawn, position);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPlacePrepSpawn(bool isMySpawn, Vector3 position)
    {
        RPC_BroadcastPlacePrepSpawn(isMySpawn, position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastPlacePrepSpawn(bool isMySpawn, Vector3 position)
    {
        PrepSpawnPlaced?.Invoke(isMySpawn, position);
    }

    public void RequestEquipmentReady()
    {
        if (Runner == null)
            return;

        if (Object.HasStateAuthority)
            SetEquipmentReady(Runner.IsServer);
        else
            RPC_RequestEquipmentReady();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestEquipmentReady(RpcInfo info = default)
    {
        SetEquipmentReady(false);
    }

    private void SetEquipmentReady(bool isHost)
    {
        if (isHost)
            hostEquipmentReady = true;
        else
            guestEquipmentReady = true;

        if (hostEquipmentReady && guestEquipmentReady)
            RPC_BroadcastEquipmentAllReady();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastEquipmentAllReady()
    {
        PrepEquipmentAllReady?.Invoke();
    }

    public void RequestBattlePlayerSpawn()
    {
        if (Runner == null)
            return;

        if (Object.HasStateAuthority)
            BattlePlayerSpawnRequested?.Invoke();
        else
            RPC_RequestBattlePlayerSpawn();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestBattlePlayerSpawn()
    {
        BattlePlayerSpawnRequested?.Invoke();
    }

    public void RecordRoundResult(int winnerSide)
    {
        if (!Object.HasStateAuthority)
            return;

        if (winnerSide == 1)
            hostRoundScore++;
        else if (winnerSide == 2)
            guestRoundScore++;

        RPC_BroadcastRoundResult(winnerSide, hostRoundScore, guestRoundScore);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastRoundResult(int winnerSide, int hostScore, int guestScore)
    {
        RoundResultAnnounced?.Invoke(winnerSide, hostScore, guestScore);
    }
}
