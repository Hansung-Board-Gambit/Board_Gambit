using Fusion;
using Fusion.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // 네트워크 관련
    public NetworkRunner runner;  //네트워크 러너
    public NetworkObject lobbyStatePrefab;  //로비스테이트 프리팹
    public LobbyState lobbystate;  //로비스테이트
    private bool isJoining = false;

    // 초기 씬 요소
    public CanvasGroup MainUI;
    public GameObject JoinPanel;  //방 코드 입력 패널
    public TMP_Text warningText;  //방 코드 에러 문구
    public TMP_InputField roomInput;  //방 코드 입력창
    public TMP_Text roomCode;  //입력된 방 코드

    // 로비 씬 요소
    public TMP_Text valueText; //승점 
    public GameObject WarningPanel; //퇴장 직전 경고 패널
    public Button startButton;  //게임 시작 버튼
    public Button readyButton;  //게임 준비 버튼
    public Button increaseButton;  //승점 증가 버튼
    public Button decreaseButton;  //승점 감소 버튼
    public TMP_Text readyButtonText;  //준비 버튼 텍스트 토글용 
    public CanvasGroup startButtonCanvas;  //시작 버튼 활성화 토글용
    public Image hostBackground;  //호스트 준비 여부 토글용
    public Image guestBackground;  //게스트 준비 여부 토글용
    public TMP_Text hostName;  //호스트 닉네임
    public TMP_Text guestName;  //게스트 닉네임
    public GameObject HostUI;  //호스트씬 UI
    public GameObject GuestUI;  //게스트씬 UI
    public CanvasGroup copyMessageGroup;  //복붙 메시지
    public float fadeDuration = 1f;  //페이드아웃 길이
    public Color readyColor = new Color(1f, 0.5f, 0.7f); // 분홍색
    public Color defaultColor = new Color(1f, 1f, 1f, 0f); // 투명

    // 옵션 씬 요소
    public string playerName = "Player";  //닉네임
    public TMP_InputField nameInput;

    // 기타 
    public PlayerSpawner spawner;  //플레이어 스포너
    public GameObject InitCanvas;  //초기 씬 캔버스
    public GameObject LobbyCanvas;  //로비 씬 캔버스
    public GameObject OptionCanvas;  //옵션 씬 캔버스
    public CanvasGroup sharingUI;   
    public CanvasGroup hostUI;
    public CanvasGroup guestUI;


    bool isExitingRoom = false;

    List<SessionInfo> sessionList = new List<SessionInfo>();

    NetworkSceneManagerDefault sceneManager;

    IEnumerator SetHostNameAfterSpawn()
    {
        yield return new WaitUntil(() => LobbyState.Instance != null);
        LobbyState.Instance.SetHostName(playerName);
    }

    IEnumerator SetGuestNameAfterSpawn()
    {
        yield return new WaitUntil(() => LobbyState.Instance != null);
        LobbyState.Instance.SetGuestName(playerName);
    }

    IEnumerator ShowCopyMessage()
    {
        copyMessageGroup.alpha = 1f;

        yield return new WaitForSeconds(1f);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            copyMessageGroup.alpha = 1f - (t / fadeDuration);
            yield return null;
        }

        copyMessageGroup.alpha = 0f;
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        playerName = PlayerPrefs.GetString("PlayerName", "Player");

        if (nameInput != null)
        {
            nameInput.text = playerName;
            nameInput.onValueChanged.AddListener(OnNameChanged);
        }

        LobbyCanvas.SetActive(false);
        OptionCanvas.SetActive(false);
        InitCanvas.SetActive(true);
    }

    string GenerateRoomID()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ01234567890123456789";
        string id = "";

        for (int i = 0; i < 6; i++)
        {
            id += chars[Random.Range(0, chars.Length)];
        }

        return id;
    }
    void SetupUI()
    {
        if (runner.IsServer)
        {
            HostUI.SetActive(true);
            GuestUI.SetActive(false);

            hostBackground.color = readyColor;
        }
        else
        {
            HostUI.SetActive(false);
            GuestUI.SetActive(true);
            hostBackground.color = readyColor;

            if (readyButton != null)
            {
                readyButton.gameObject.SetActive(true);
                readyButton.interactable = true;
            }

            if (readyButtonText != null)
                readyButtonText.text = "Ready";
        }
    }

    // Play as host 버튼
    public async void StartHost()
    {
        MainUI.interactable = false;
        MainUI.blocksRaycasts = false;

        ResetRunner();

        runner = new GameObject("NetworkRunner").AddComponent<NetworkRunner>();
        DontDestroyOnLoad(runner.gameObject);
        //hibox 컴포넌트 추가
        runner.gameObject.AddComponent<HitboxManager>();
        runner.ProvideInput = true;

        runner.AddCallbacks(this);

        string roomID = GenerateRoomID();
        roomCode.text = "Room Code: " + roomID;

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomID,
            PlayerCount = 2,
            SceneManager = sceneManager
        });

        if (!result.Ok)
        {
            Debug.LogError("Host 생성 실패");
            ResetRunner();
            MainUI.interactable = true;
            MainUI.blocksRaycasts = true;
            return;
        }

        NetworkObject obj = runner.Spawn(lobbyStatePrefab, Vector3.zero, Quaternion.identity);
        DontDestroyOnLoad(obj.gameObject);
        lobbystate = obj.GetComponent<LobbyState>();

        StartCoroutine(SetHostNameAfterSpawn());

        InitCanvas.SetActive(false);
        LobbyCanvas.SetActive(true);

        SetupUI();

        SetStartButton(false);

        Debug.Log("Host 시작");
    }

    //룸코드에 맞게 서버 연결하기
    public void Connect()
    {
        string roomID = roomInput.text.Trim().ToUpper();

        Debug.Log("입력된 RoomCode: " + roomID);

        StartClient(roomID);
    }

    //Play as guest 버튼
    public async void StartClient(string roomID)
    {
        if (isJoining) return;
        isJoining = true;

        Debug.Log("StartClient 받은 코드: " + roomID);

        ResetRunner();

        runner = new GameObject("NetworkRunner").AddComponent<NetworkRunner>();
        DontDestroyOnLoad(runner.gameObject);
        //hitbox컴포넌트 추가 
        runner.gameObject.AddComponent<HitboxManager>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = roomID,
            SceneManager = sceneManager,
        });

        Debug.Log($"Join Result: " + $"Ok={result.Ok}, " + $"Reason={result.ShutdownReason}");

        if (!result.Ok)
        {
            Debug.LogError("Wrong room code");

            if (result.ShutdownReason == ShutdownReason.GameIsFull) { 
                warningText.text = "Room is full"; 
            }
            else {
                warningText.text = "Room not found";
            }

            roomInput.Select();
            roomInput.ActivateInputField();
            ResetRunner();
            MainUI.interactable = false;
            MainUI.blocksRaycasts = false;
            isJoining = false;
            return;
        }

        warningText.text = "";
        JoinPanel.SetActive(false);

        StartCoroutine(SetGuestNameAfterSpawn());

        InitCanvas.SetActive(false);
        LobbyCanvas.SetActive(true);

        roomCode.text = "Room Code: " + runner.SessionInfo.Name;

        SetupUI();
        isJoining = false;

        Debug.Log("서버 접속 성공 (Client)");
    }

    public void OnReadyButtonClicked()
    {
        Debug.Log("button clicked");

        if (runner == null || runner.IsServer) return;

        if (LobbyState.Instance == null)
        {
            Debug.Log("LobbyState 아직 없음");
            return;
        }

        LobbyState.Instance.ToggleGuestReady();
    }

    public void GameStart()
    {
        if (!runner.IsServer) return;

        if (lobbystate == null) return;

        Debug.Log("게임 시작");

        lobbystate.StartGame();
    }

    // Exit 버튼

    public void ReturnToLobbyAfterMatch(float delaySeconds)
    {
        if (isExitingRoom)
            return;

        StartCoroutine(ReturnToLobbyAfterMatchRoutine(delaySeconds));
    }

    private IEnumerator ReturnToLobbyAfterMatchRoutine(float delaySeconds)
    {
        isExitingRoom = true;

        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        NetworkRunner currentRunner = runner;
        if (currentRunner != null && currentRunner.IsRunning)
        {
            currentRunner.RemoveCallbacks(this);
            var shutdownTask = currentRunner.Shutdown();
            while (!shutdownTask.IsCompleted)
                yield return null;
        }

        if (currentRunner != null)
            Destroy(currentRunner.gameObject);

        runner = null;
        lobbystate = null;

        if (LobbyState.Instance != null)
            Destroy(LobbyState.Instance.gameObject);

        GameInputGate.Unlock();
        SceneManager.LoadScene("Jinsoo2");
        Destroy(gameObject);
    }

    public async void ExitRoom()
    {
        if (isExitingRoom)
            return;

        isExitingRoom = true;

        try
        {
            NetworkRunner currentRunner = runner;
            if (currentRunner != null && currentRunner.IsRunning)
            {
                currentRunner.RemoveCallbacks(this);
                await currentRunner.Shutdown();
            }

            if (currentRunner != null)
                Destroy(currentRunner.gameObject);

            if (runner == currentRunner)
                runner = null;

            SetCanvasGroupInteractable(sharingUI, true);
            SetCanvasGroupInteractable(hostUI, true);
            SetCanvasGroupInteractable(guestUI, true);

            SetActiveIfExists(LobbyCanvas, false);
            SetActiveIfExists(InitCanvas, true);
            SetActiveIfExists(WarningPanel, false);

            MainUI.interactable = true;
            MainUI.blocksRaycasts = true;

            if (roomCode != null)
                roomCode.text = "Room Code : ";

            if (roomInput != null)
                roomInput.text = "";

            ResetLobbyUI();

            Debug.Log("방에서 나감");
        }
        finally
        {
            isExitingRoom = false;
        }
    }

    void ResetLobbyUI()
    {
        if (hostName != null)
            hostName.text = "Host name";

        if (guestName != null)
            guestName.text = "";

        if (readyButton != null)
            readyButton.interactable = false;

        if (startButton != null)
            startButton.interactable = false;
    }

    void SetCanvasGroupInteractable(CanvasGroup group, bool interactable)
    {
        if (group == null)
            return;

        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }

    void SetActiveIfExists(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    // Runner 초기화
    void ResetRunner()
    {
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
            Destroy(runner.gameObject);
            runner = null;
        }
    }

    // Host 종료 감지
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("Host 종료됨: " + shutdownReason);

        if (!isExitingRoom)
            ExitRoom();
    }
    public void OnIncreaseButton()
    {
        if (!runner.IsServer) return;

        if (lobbystate.gameValue < 5)
            lobbystate.gameValue++;
    }

    public void OnDecreaseButton()
    {
        if (!runner.IsServer) return;

        if (lobbystate.gameValue > 2)
            lobbystate.gameValue--;
    }

    void Update()
    {
        if (LobbyState.Instance == null) return;

        var state = LobbyState.Instance;

        if (!state.Object || !state.Object.IsValid) return;

        valueText.text = state.gameValue.ToString();
        decreaseButton.interactable = state.gameValue > 2;
        increaseButton.interactable = state.gameValue < 5;

        hostName.text = string.IsNullOrEmpty(state.hostName.ToString())
        ? ""
        : state.hostName.ToString();

        if (string.IsNullOrEmpty(state.guestName.ToString()))
        {
            guestName.text = "";
        }
        else
        {
            guestName.text = state.guestName.ToString();
        }

        if (runner != null && runner.IsServer)
        {
            SetStartButton(state.guestReady);
        }

        UpdateGuestUI(state.guestReady);
    }

    public void UpdateGuestUI(bool isReady)
    {
        if (isReady)
        {
            guestBackground.color = readyColor;
            readyButtonText.text = "Unready";
        }
        else
        {
            guestBackground.color = defaultColor;
            readyButtonText.text = "Ready";
        }
    }

    public void CopyRoomCode()
    {
        if (runner == null || runner.SessionInfo == null) return;

        string code = runner.SessionInfo.Name;

        GUIUtility.systemCopyBuffer = code;

        StopAllCoroutines();
        StartCoroutine(ShowCopyMessage());

        Debug.Log("룸코드 복사됨: " + code);
    }

    void SetStartButton(bool enable)
    {
        startButton.interactable = enable;

        if (enable)
            startButtonCanvas.alpha = 1f;
        else
            startButtonCanvas.alpha = 0.5f;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("플레이어 입장: " + player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (LobbyState.Instance == null) return;

        if (runner.IsServer)
        {
            LobbyState.Instance.guestName = "";
            LobbyState.Instance.ResetGuestReady();
        }

        if (startButton != null)
            startButton.interactable = false;
    }

    public void OnNameChanged(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        Debug.Log("입력된 이름: " + name);

        playerName = name;

        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) 
    {
        var data = new NetworkInputData();

        if (!GameInputGate.AllowPlayerInput)
        {
            input.Set(data);
            return;
        }

        if (Input.GetKey(KeyCode.W))
            data.direction += Vector3.forward;

        if (Input.GetKey(KeyCode.S))
            data.direction += Vector3.back;

        if (Input.GetKey(KeyCode.A))
            data.direction += Vector3.left;

        if (Input.GetKey(KeyCode.D))
            data.direction += Vector3.right;

        data.yaw = Player.NetworkedYaw;
        data.pitch = Player.NetworkedPitch;

        data.jump = Input.GetKey(KeyCode.Space);
        //누르고 있으면  true, 떼면 false
        //속도변환
        data.speedUp = Input.GetKey(KeyCode.LeftShift);
        //앉기
        data.sitDown = Input.GetKey(KeyCode.LeftControl);
     
        data.buttons.Set(MyButtons.LeftClick, Input.GetMouseButton(0));
        data.buttons.Set(MyButtons.RightClick, Input.GetMouseButton(1));
        data.buttons.Set(MyButtons.SkillQ, Input.GetKey(KeyCode.Q));
        data.buttons.Set(MyButtons.Reload, Input.GetKey(KeyCode.R));
        input.Set(data);
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessions)
    {
        sessionList = sessions;
    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}