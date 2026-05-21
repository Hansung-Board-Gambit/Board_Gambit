using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameRoundPhase
{
    Preparation,
    Countdown,
    Battle,
    RoundResult,
    MatchResult
}

public class GameRoundFlowController : MonoBehaviour
{
    [Header("Flow")]
    public PrepPhaseFlowUI prepFlow;
    public PrepDataStore prepDataStore;
    public SpawnPlacementManager spawnPlacementManager;
    public int roundIndex = 1;
    public float countdownDuration = 3f;
    public float roundDuration = 60f;
    public float roundResultDuration = 3f;
    public float matchResultDuration = 5f;
    public bool autoStartNextRoundOnTimer = true;
    public bool returnToLobbyAfterMatch = true;
    public bool showDebugOverlay = true;

    [Header("Camera")]
    public Camera preparationCamera;

    [Header("Battle Spawning")]
    public GameObject playerPrefabObject;
    public NetworkPrefabRef playerPrefab;
    public float playerSpawnYOffset = 1f;
    public Vector3 fallbackHostSpawn = new Vector3(-2f, 1f, 0f);
    public Vector3 fallbackGuestSpawn = new Vector3(2f, 1f, 0f);

    [Header("UI")]
    public GameObject preparationRoot;
    public GameObject battleHudRoot;
    public CanvasGroup countdownCanvasGroup;
    public Text countdownText;
    public Text phaseText;
    public Image battleTimerFill;
    public TextMeshProUGUI battleTimerText;
    public TextMeshProUGUI roundResultText;
    public TextMeshProUGUI matchScoreText;

    public GameRoundPhase CurrentPhase { get; private set; } = GameRoundPhase.Preparation;

    private Coroutine battleRoutine;
    private Coroutine battleSpawnRoutine;
    private float battleTimeRemaining;
    private float countdownTimeRemaining;
    private int latestRoundWinnerSide = -1;
    private int latestHostScore;
    private int latestGuestScore;
    private Coroutine roundResultRoutine;
    private GameObject playerUiRoot;
    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        if (prepFlow == null)
            prepFlow = GetComponent<PrepPhaseFlowUI>();

        if (prepDataStore == null)
            prepDataStore = GetComponent<PrepDataStore>();

        if (spawnPlacementManager == null)
            spawnPlacementManager = FindObjectOfType<SpawnPlacementManager>();

        if (preparationCamera == null)
            preparationCamera = Camera.main;

        GameInputGate.Unlock();
        EnsureBattleHudUi();
        SetPreparationCameraVisible(true);
        SetLocalBattleControlActive(false);
        SetBattleHudVisible(false);
        SetCountdownVisible(false);
        UpdatePhaseText();
    }

    private void OnEnable()
    {
        if (prepFlow != null)
            prepFlow.PrepFlowCompleted += HandlePrepFlowCompleted;

        LobbyState.BattlePlayerSpawnRequested += HandleBattlePlayerSpawnRequested;
        LobbyState.RoundResultAnnounced += HandleRoundResultAnnounced;
    }

    private void OnDisable()
    {
        if (prepFlow != null)
            prepFlow.PrepFlowCompleted -= HandlePrepFlowCompleted;

        LobbyState.BattlePlayerSpawnRequested -= HandleBattlePlayerSpawnRequested;
        LobbyState.RoundResultAnnounced -= HandleRoundResultAnnounced;
    }

    private void HandlePrepFlowCompleted()
    {
        string side = LobbyState.Instance != null && LobbyState.Instance.Runner != null && LobbyState.Instance.Runner.IsServer
            ? "Host"
            : "Client";
        Debug.Log("Preparation flow completed on " + side + ". Starting battle sequence.");
        StartBattleSequence();
    }

    public void StartBattleSequence()
    {
        if (battleRoutine != null)
            StopCoroutine(battleRoutine);

        battleRoutine = StartCoroutine(BattleSequenceRoutine());
    }

    public void StartNextPreparation()
    {
        roundIndex++;

        if (LobbyState.Instance != null)
            LobbyState.Instance.AdvancePrepRound();

        if (prepDataStore != null)
            prepDataStore.ResetRoundPlacementPoints();

        CurrentPhase = GameRoundPhase.Preparation;
        GameInputGate.Unlock();
        SetLocalBattleControlActive(false);
        SetPreparationCameraVisible(true);
        SetSpawnMarkersVisible(true);
        SetBattleHudVisible(false);
        SetCountdownVisible(false);
        UpdatePhaseText();

        if (preparationRoot != null)
            preparationRoot.SetActive(true);

        if (prepFlow != null)
            prepFlow.BeginFlow();
    }

    public void CompleteRoundAndPrepareNext()
    {
        if (CurrentPhase != GameRoundPhase.Battle)
            return;

        if (!CanResolveRound())
            return;

        AnnounceRoundResultIfHost();
    }

    private IEnumerator BattleSequenceRoutine()
    {
        CurrentPhase = GameRoundPhase.Countdown;
        latestRoundWinnerSide = -1;
        if (roundResultText != null)
            roundResultText.gameObject.SetActive(false);
        GameInputGate.Lock();
        UpdatePhaseText();

        if (preparationRoot != null)
            preparationRoot.SetActive(false);

        SetBattleHudVisible(true);
        SetSpawnMarkersVisible(false);
        SetCountdownVisible(true);

        float remain = Mathf.Max(0.1f, countdownDuration);
        while (remain > 0f)
        {
            countdownTimeRemaining = remain;

            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(remain).ToString();

            remain -= Time.deltaTime;
            yield return null;
        }

        if (countdownText != null)
            countdownText.text = "Fight!";

        yield return new WaitForSeconds(0.75f);

        CurrentPhase = GameRoundPhase.Battle;
        RequestBattlePlayerSpawn();
        SetPreparationCameraVisible(false);
        SetLocalBattleControlActive(true);
        SetCountdownVisible(false);
        GameInputGate.Unlock();
        Debug.Log("Battle phase started. Player input unlocked.");
        UpdatePhaseText();
        StartCoroutine(RefreshLocalBattleUiAfterSpawn());

        yield return RunBattleTimerRoutine();
        battleRoutine = null;
    }

    private IEnumerator RunBattleTimerRoutine()
    {
        float duration = Mathf.Max(0.1f, roundDuration);
        float remain = duration;
        bool canResolveRound = CanResolveRound();

        while (remain > 0f && CurrentPhase == GameRoundPhase.Battle)
        {
            remain -= Time.deltaTime;
            battleTimeRemaining = Mathf.Max(0f, remain);

            if (battleTimerFill != null)
                battleTimerFill.fillAmount = Mathf.Clamp01(remain / duration);

            if (battleTimerText != null)
                battleTimerText.text = "Time: " + Mathf.CeilToInt(battleTimeRemaining);

            if (canResolveRound && ShouldEndRoundByHealth())
                break;

            yield return null;
        }

        if (canResolveRound && CurrentPhase == GameRoundPhase.Battle && autoStartNextRoundOnTimer)
            CompleteRoundAndPrepareNext();
    }

    private IEnumerator CompleteRoundRoutine(int winnerSide)
    {
        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
            battleRoutine = null;
        }

        latestRoundWinnerSide = winnerSide;
        CurrentPhase = GameRoundPhase.RoundResult;
        GameInputGate.Lock();
        SetLocalBattleControlActive(false);
        Debug.Log("Round completed.");
        UpdatePhaseText();
        SetBattleHudVisible(true);
        UpdateRoundResultText();

        yield return new WaitForSeconds(roundResultDuration);

        if (ShouldStartNextRound())
        {
            Debug.Log("Returning to next preparation.");
            StartNextPreparation();
        }
        else
        {
            CurrentPhase = GameRoundPhase.MatchResult;
            GameInputGate.Lock();
            SetLocalBattleControlActive(false);
            SetPreparationCameraVisible(true);
            SetSpawnMarkersVisible(false);
            SetBattleHudVisible(true);
            SetCountdownVisible(false);
            Debug.Log("Match completed after round " + roundIndex + ".");
            UpdatePhaseText();
            UpdateRoundResultText();

            if (returnToLobbyAfterMatch)
                ReturnToLobbyAfterMatchDelay();
        }

        roundResultRoutine = null;
    }

    private void SetBattleHudVisible(bool visible)
    {
        if (visible)
            EnsureBattleHudUi();

        if (battleHudRoot != null)
            battleHudRoot.SetActive(visible);

        if (playerUiRoot != null)
            playerUiRoot.SetActive(visible);

        if (!visible)
        {
            if (roundResultText != null)
                roundResultText.gameObject.SetActive(false);

            if (PlayerUI.instance != null)
                PlayerUI.instance.Clear();
        }
    }

    private void EnsureBattleHudUi()
    {
        if (PlayerUI.instance == null)
        {
            playerUiRoot = CreatePlayerUiRoot();
            PlayerUI playerUi = playerUiRoot.AddComponent<PlayerUI>();
            TextMeshProUGUI hpText = CreatePlayerUiText(
                "PlayerHP",
                playerUiRoot.transform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(25f, 50f),
                new Vector2(400f, 50f),
                TextAlignmentOptions.Left,
                36,
                "HP : ");
            TextMeshProUGUI ammoText = CreatePlayerUiText(
                "Ammo",
                playerUiRoot.transform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-100f, 25f),
                new Vector2(400f, 50f),
                TextAlignmentOptions.Left,
                36,
                "Ammo : ");
            playerUi.Configure(hpText, ammoText);
        }
        else if (playerUiRoot == null)
        {
            playerUiRoot = PlayerUI.instance.gameObject;
        }

        if (battleHudRoot == null)
            return;

        if (battleTimerText == null)
            battleTimerText = CreateHudText("BattleTimerText", battleHudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(520f, 60f), TextAlignmentOptions.Center, 28);

        if (roundResultText == null)
        {
            roundResultText = CreateHudText("RoundResultText", battleHudRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(720f, 90f), TextAlignmentOptions.Center, 42);
            roundResultText.gameObject.SetActive(false);
        }

        if (matchScoreText == null)
            matchScoreText = CreateHudText("MatchScoreText", battleHudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(520f, 44f), TextAlignmentOptions.Center, 24);
    }

    private GameObject CreatePlayerUiRoot()
    {
        GameObject uiObject = new GameObject("PlayerUI");
        uiObject.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = uiObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Canvas canvas = uiObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = uiObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        uiObject.AddComponent<GraphicRaycaster>();
        return uiObject;
    }

    private TextMeshProUGUI CreatePlayerUiText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment, int fontSize, string initialText)
    {
        TextMeshProUGUI text = CreateHudText(objectName, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, alignment, fontSize);
        text.text = initialText;
        return text;
    }

    private TextMeshProUGUI CreateHudText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment, int fontSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = "";
        return text;
    }

    private bool ShouldStartNextRound()
    {
        int targetRoundCount = LobbyState.Instance != null ? Mathf.Max(2, LobbyState.Instance.gameValue) : 2;
        return roundIndex < targetRoundCount;
    }

    private void SetSpawnMarkersVisible(bool visible)
    {
        if (spawnPlacementManager != null)
            spawnPlacementManager.SetMarkersVisible(visible);
    }

    private void SetPreparationCameraVisible(bool visible)
    {
        if (preparationCamera != null)
            preparationCamera.gameObject.SetActive(visible);
    }

    private void SetLocalBattleControlActive(bool active)
    {
        Player[] players = FindObjectsOfType<Player>(true);
        for (int i = 0; i < players.Length; i++)
            players[i].SetBattleControlActive(active);
    }

    private void SetCountdownVisible(bool visible)
    {
        if (countdownCanvasGroup == null)
            return;

        countdownCanvasGroup.gameObject.SetActive(visible);
        countdownCanvasGroup.alpha = visible ? 1f : 0f;
        countdownCanvasGroup.interactable = false;
        countdownCanvasGroup.blocksRaycasts = visible;
    }

    private void UpdatePhaseText()
    {
        if (phaseText != null)
            phaseText.text = "Round " + roundIndex + " - " + CurrentPhase;

        if (matchScoreText != null)
            matchScoreText.text = "Round " + roundIndex + " / Score " + latestHostScore + " - " + latestGuestScore;
    }

    private bool ShouldEndRoundByHealth()
    {
        if (!TryGetPlayerHealth(1, out PlayerHealth hostHealth) || !TryGetPlayerHealth(2, out PlayerHealth guestHealth))
            return false;

        return hostHealth.CurrentHP <= 0 || guestHealth.CurrentHP <= 0;
    }

    private void AnnounceRoundResultIfHost()
    {
        if (LobbyState.Instance == null || LobbyState.Instance.Runner == null || !LobbyState.Instance.Runner.IsServer)
            return;

        int winnerSide = DetermineRoundWinner();
        LobbyState.Instance.RecordRoundResult(winnerSide);
    }

    private int DetermineRoundWinner()
    {
        bool hasHost = TryGetPlayerHealth(1, out PlayerHealth hostHealth);
        bool hasGuest = TryGetPlayerHealth(2, out PlayerHealth guestHealth);

        if (!hasHost && !hasGuest)
            return 0;

        int hostHp = hasHost ? hostHealth.CurrentHP : 0;
        int guestHp = hasGuest ? guestHealth.CurrentHP : 0;

        if (hostHp == guestHp)
            return 0;

        return hostHp > guestHp ? 1 : 2;
    }

    private bool TryGetPlayerHealth(int playerId, out PlayerHealth health)
    {
        foreach (NetworkObject playerObject in spawnedPlayers.Values)
        {
            if (playerObject == null || !playerObject.IsValid || playerObject.InputAuthority.PlayerId != playerId)
                continue;

            health = playerObject.GetComponent<PlayerHealth>();
            return health != null;
        }

        health = null;
        return false;
    }

    private void HandleRoundResultAnnounced(int winnerSide, int hostScore, int guestScore)
    {
        latestRoundWinnerSide = winnerSide;
        latestHostScore = hostScore;
        latestGuestScore = guestScore;

        if (roundResultRoutine != null)
            StopCoroutine(roundResultRoutine);

        roundResultRoutine = StartCoroutine(CompleteRoundRoutine(winnerSide));
    }

    private void UpdateRoundResultText()
    {
        EnsureBattleHudUi();

        if (roundResultText == null)
            return;

        string winnerText = latestRoundWinnerSide == 1
            ? "Host Wins Round"
            : latestRoundWinnerSide == 2
                ? "Guest Wins Round"
                : "Round Draw";

        if (CurrentPhase == GameRoundPhase.MatchResult)
        {
            if (latestHostScore == latestGuestScore)
                winnerText = "Match Draw";
            else
                winnerText = latestHostScore > latestGuestScore ? "Host Wins Match" : "Guest Wins Match";

            winnerText += "\nScore " + latestHostScore + " - " + latestGuestScore;
        }
        else
        {
            winnerText += "\nScore " + latestHostScore + " - " + latestGuestScore;
        }

        roundResultText.text = winnerText;
        roundResultText.gameObject.SetActive(CurrentPhase == GameRoundPhase.RoundResult || CurrentPhase == GameRoundPhase.MatchResult);
    }

    private bool CanResolveRound()
    {
        return LobbyState.Instance != null
            && LobbyState.Instance.Runner != null
            && LobbyState.Instance.Runner.IsServer;
    }

    private void ReturnToLobbyAfterMatchDelay()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
            networkManager.ReturnToLobbyAfterMatch(matchResultDuration);
        else
            Debug.LogWarning("Cannot return to lobby because NetworkManager is missing.");
    }

    private void RequestBattlePlayerSpawn()
    {
        if (LobbyState.Instance == null)
        {
            Debug.LogWarning("Cannot request battle player spawn because LobbyState is missing.");
            return;
        }

        Debug.Log("Requesting battle player spawn through LobbyState.");
        LobbyState.Instance.RequestBattlePlayerSpawn();
    }

    private void HandleBattlePlayerSpawnRequested()
    {
        if (battleSpawnRoutine != null)
            StopCoroutine(battleSpawnRoutine);

        battleSpawnRoutine = StartCoroutine(SpawnBattlePlayersWhenReady());
    }

    private IEnumerator SpawnBattlePlayersWhenReady()
    {
        const int maxAttempts = 20;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (SpawnOrRepositionBattlePlayers())
            {
                battleSpawnRoutine = null;
                yield break;
            }

            Debug.LogWarning("Battle player spawn attempt " + attempt + " failed. Retrying shortly.");
            yield return new WaitForSeconds(0.25f);
        }

        Debug.LogError("Battle player spawning failed after repeated attempts.");
        battleSpawnRoutine = null;
    }

    private bool SpawnOrRepositionBattlePlayers()
    {
        if (LobbyState.Instance == null || LobbyState.Instance.Runner == null)
        {
            Debug.LogWarning("Cannot spawn battle players because LobbyState or Runner is missing.");
            return false;
        }

        NetworkRunner runner = LobbyState.Instance.Runner;
        Debug.Log("SpawnOrRepositionBattlePlayers called. IsServer=" + runner.IsServer + ", IsRunning=" + runner.IsRunning);
        if (!runner.IsServer)
        {
            Debug.Log("Battle player spawning skipped on client.");
            return true;
        }

        bool hasGameObjectPrefab = playerPrefabObject != null;
        bool hasNetworkPrefabRef = playerPrefab.IsValid;
        if (!hasGameObjectPrefab && !hasNetworkPrefabRef)
        {
            Debug.LogWarning("Cannot spawn battle players because no player prefab is assigned.");
            return false;
        }

        int activePlayerCount = runner.ActivePlayers.Count();
        Debug.Log("Spawning battle players. ActivePlayers=" + activePlayerCount);
        if (activePlayerCount == 0)
            return false;

        bool spawnedAllPlayers = true;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            Vector3 spawnPosition = GetBattleSpawnPosition(player);
            Debug.Log("Preparing battle spawn for " + player + " at " + spawnPosition);

            NetworkObject existingPlayer;
            if (spawnedPlayers.TryGetValue(player, out existingPlayer) && existingPlayer != null && existingPlayer.IsValid)
            {
                Player existingPlayerController = existingPlayer.GetComponent<Player>();
                if (existingPlayerController != null)
                    existingPlayerController.TeleportForBattle(spawnPosition, Quaternion.identity);
                else
                    existingPlayer.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

                ResetPlayerForRound(existingPlayer);
                ApplySelectedWeapon(player, existingPlayer);
                Debug.Log("Repositioned existing battle player for " + player);
                continue;
            }

            NetworkObject spawned = hasGameObjectPrefab
                ? runner.Spawn(playerPrefabObject, spawnPosition, Quaternion.identity, player)
                : runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            if (spawned != null)
            {
                spawnedPlayers[player] = spawned;
                runner.SetPlayerObject(player, spawned);
                ResetPlayerForRound(spawned);
                ApplySelectedWeapon(player, spawned);
                Debug.Log("Spawned battle player for " + player + ": " + spawned.name);
            }
            else
            {
                Debug.LogWarning("Runner.Spawn returned null for " + player);
                spawnedAllPlayers = false;
            }
        }

        return spawnedAllPlayers;
    }

    private void ResetPlayerForRound(NetworkObject playerObject)
    {
        PlayerHealth health = playerObject != null ? playerObject.GetComponent<PlayerHealth>() : null;
        if (health != null)
            health.ResetForRound();
    }

    private void ApplySelectedWeapon(PlayerRef player, NetworkObject playerObject)
    {
        if (playerObject == null || LobbyState.Instance == null || prepFlow == null)
            return;

        int equipmentIndex = LobbyState.Instance.GetSelectedEquipmentIndex(player);
        WeaponData selectedWeapon = prepFlow.GetEquipmentByIndex(equipmentIndex);
        if (selectedWeapon == null)
        {
            Debug.LogWarning("No selected weapon found for " + player + ". Index=" + equipmentIndex);
            return;
        }

        PlayerWeapon playerWeapon = playerObject.GetComponent<PlayerWeapon>();
        if (playerWeapon == null)
        {
            Debug.LogWarning("Cannot apply selected weapon because PlayerWeapon is missing on " + playerObject.name);
            return;
        }

        playerWeapon.SetWeaponDataAndEquip(selectedWeapon);
        Debug.Log("Applied selected weapon to " + player + ": " + selectedWeapon.name);
    }

    private IEnumerator RefreshLocalBattleUiAfterSpawn()
    {
        yield return null;
        RefreshLocalBattleUi();
        yield return new WaitForSeconds(0.25f);
        RefreshLocalBattleUi();
    }

    private void RefreshLocalBattleUi()
    {
        if (PlayerUI.instance == null)
            return;

        PlayerHealth[] healths = FindObjectsOfType<PlayerHealth>(true);
        for (int i = 0; i < healths.Length; i++)
        {
            PlayerHealth health = healths[i];
            if (health != null && health.HasInputAuthority)
                PlayerUI.instance.UpdateHPText(health.CurrentHP, health.maxHP);
        }

        WeaponBase[] weapons = FindObjectsOfType<WeaponBase>(true);
        for (int i = 0; i < weapons.Length; i++)
        {
            WeaponBase weapon = weapons[i];
            if (weapon != null && weapon.HasInputAuthority)
                weapon.OnAmmoUIChanged();
        }
    }

    private Vector3 GetBattleSpawnPosition(PlayerRef player)
    {
        bool isHostPlayer = player.PlayerId == 1;
        bool spawnOwnerIsHost = LobbyState.Instance == null || LobbyState.Instance.objectPlacementAuthorityIsHost;

        bool hasMySpawn = prepDataStore != null && prepDataStore.spawnData != null && prepDataStore.spawnData.hasMySpawn;
        bool hasOpponentSpawn = prepDataStore != null && prepDataStore.spawnData != null && prepDataStore.spawnData.hasOpponentSpawn;

        Vector3 hostSpawn = fallbackHostSpawn;
        Vector3 guestSpawn = fallbackGuestSpawn;

        if (hasMySpawn)
        {
            Vector3 position = prepDataStore.spawnData.mySpawnPosition + Vector3.up * playerSpawnYOffset;
            if (spawnOwnerIsHost)
                hostSpawn = position;
            else
                guestSpawn = position;
        }

        if (hasOpponentSpawn)
        {
            Vector3 position = prepDataStore.spawnData.opponentSpawnPosition + Vector3.up * playerSpawnYOffset;
            if (spawnOwnerIsHost)
                guestSpawn = position;
            else
                hostSpawn = position;
        }

        return isHostPlayer ? hostSpawn : guestSpawn;
    }

    private void OnGUI()
    {
        if (!showDebugOverlay)
            return;

        string extra = "";
        if (CurrentPhase == GameRoundPhase.Countdown)
            extra = " / Countdown: " + Mathf.CeilToInt(countdownTimeRemaining);
        else if (CurrentPhase == GameRoundPhase.Battle)
            extra = " / Battle Time: " + Mathf.CeilToInt(battleTimeRemaining);

        string authority = "";
        if (LobbyState.Instance != null)
        {
            authority = " / " + LobbyState.Instance.GetLocalAuthorityDebugText();
        }
        else
        {
            authority = " / LobbyState missing";
        }

        GUI.Label(
            new Rect(12, 12, 780, 28),
            "Round " + roundIndex + " / " + CurrentPhase + extra + authority + " / Input: " + GameInputGate.AllowPlayerInput
        );
    }
}
