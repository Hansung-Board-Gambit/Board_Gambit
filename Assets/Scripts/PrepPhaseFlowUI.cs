using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrepPhaseFlowUI : MonoBehaviour
{
    public event Action PrepFlowCompleted;

    [Header("Turn Intro SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip prepStartSfx;

    [Header("Warning SFX")]
    [SerializeField] AudioSource warningAudioSource;
    [SerializeField] private AudioClip timerWarningSfx;

    [Header("Flow")]
    public bool playOnStart = true;

    [Header("Editor Test")]
    public bool allowEditorLocalTest = false;

    [Header("Turn Intro Overlay")]
    public CanvasGroup turnIntroCanvasGroup;
    public Text turnIntroText;

    [Header("Turn Intro Settings")]
    public float turnIntroFadeDuration = 3f;
    public string objectPlacementPlayerName = "Host";
    public string spawnPlacementPlayerName = "Guest";

    [Header("Phase Panels")]
    public GameObject objectPlacementPanel;
    public GameObject spawnPlacementPanel;
    public GameObject equipmentSelectionPanel;
    public PlacementManager placementManager;

    [Header("Phase Durations")]
    public float objectPlacementDuration = 10f;
    public float spawnPlacementDuration = 10f;
    public float equipmentSelectionDuration = 10f;

    [Header("Timer Fill Images")]
    public Image objectPlacementTimerFill;
    public Image spawnPlacementTimerFill;
    public Image equipmentSelectionTimerFill;

    [Header("Buttons")]
    public Button objectPlacementFinishButton;
    public Button spawnPlacementFinishButton;
    public Button equipmentSelectionFinishButton;
    public Button equipmentSkipButton;

    [Header("Equipment Selection")]
    public PrepDataStore dataStore;
    public WeaponData[] equipmentPool;
    //public Transform[] equipmentCards;
    public EquipmentCardUI[] equipmentCards;
    public Color equipmentCardNormalColor = new Color(0f, 0f, 0f, 0.392f);
    public Color equipmentCardSelectedColor = new Color(0.15f, 0.45f, 0.85f, 0.75f);
    //무작위 3개 담을 임시 리스트
    private List<WeaponData> currentRandomPool = new List<WeaponData>();

    private bool warningPlayed;
    private bool skipRequested;
    private bool equipmentAllReady;
    private int currentPhaseIndex = -1;
    private int selectedEquipmentIndex = -1;
    private Coroutine flowRoutine;
    private readonly List<Button> equipmentButtons = new List<Button>();

    private void OnEnable()
    {
        LobbyState.PrepPhaseSkipRequested += HandleNetworkSkipRequested;
        LobbyState.PrepEquipmentAllReady += HandleEquipmentAllReady;
    }

    private void OnDisable()
    {
        LobbyState.PrepPhaseSkipRequested -= HandleNetworkSkipRequested;
        LobbyState.PrepEquipmentAllReady -= HandleEquipmentAllReady;
    }

    private void Awake()
    {
        if (dataStore == null)
            dataStore = GetComponent<PrepDataStore>();

        if (placementManager == null)
            placementManager = FindFirstObjectByType<PlacementManager>();

        if (turnIntroCanvasGroup != null)
        {
            turnIntroCanvasGroup.alpha = 0f;
            turnIntroCanvasGroup.interactable = false;
            turnIntroCanvasGroup.blocksRaycasts = false;
            turnIntroCanvasGroup.gameObject.SetActive(false);
        }

        HideAllPanels();
    }

    private void Start()
    {
        BindButtons();

        if (playOnStart)
            BeginFlow();
    }

    //겹치지 않게 3개 뽑아내기
    private void GenerateRandomEquipmentPool()
    {
        currentRandomPool.Clear();
        List<WeaponData> tempPool = new List<WeaponData>(equipmentPool);

        for (int i = 0; i < 3; i++)
        {
            if (tempPool.Count == 0) break;
            int randomIndex = UnityEngine.Random.Range(0, tempPool.Count);
            currentRandomPool.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex); // 중복 방지
        }
    }

    public void BeginFlow()
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        GenerateRandomEquipmentPool();
        RefreshObjectPlacementSlotsForRound();

        skipRequested = false;
        equipmentAllReady = false;
        flowRoutine = StartCoroutine(MainFlow());
    }

    private void BindButtons()
    {
        if (objectPlacementFinishButton != null)
        {
            objectPlacementFinishButton.onClick.RemoveAllListeners();
            objectPlacementFinishButton.onClick.AddListener(SkipCurrentPhase);
        }

        if (spawnPlacementFinishButton != null)
        {
            spawnPlacementFinishButton.onClick.RemoveAllListeners();
            spawnPlacementFinishButton.onClick.AddListener(SkipCurrentPhase);
        }

        if (equipmentSelectionFinishButton != null)
        {
            equipmentSelectionFinishButton.onClick.RemoveAllListeners();
            equipmentSelectionFinishButton.onClick.AddListener(SkipCurrentPhase);
        }

        if (equipmentSkipButton != null)
        {
            equipmentSkipButton.onClick.RemoveAllListeners();
            equipmentSkipButton.onClick.AddListener(SkipCurrentPhase);
        }

        BindEquipmentCards();
    }

    private IEnumerator MainFlow()
    {
        currentPhaseIndex = 0;
        // 1단계 화면 먼저 띄우기
        ShowOnlyPanel(objectPlacementPanel);
        yield return WaitForPrepAuthorityReadyRoutine();

        // 그 위에 턴 소개 오버레이
        FindFirstObjectByType<GameRoundFlowController>()?.StartPrepBgm();
        yield return ShowTurnIntroRoutine(GetObjectPlacementTurnText);

        // 오버레이가 사라진 뒤 타이머 진행
        yield return RunPhaseTimerRoutine(currentPhaseIndex, objectPlacementDuration, objectPlacementTimerFill);
        yield return WaitForObjectPlacementCompleteRoutine();

        currentPhaseIndex = 1;
        // 2단계 화면 먼저 띄우기
        ShowOnlyPanel(spawnPlacementPanel);
        yield return WaitForPrepAuthorityReadyRoutine();

        // 그 위에 턴 소개 오버레이
        yield return ShowTurnIntroRoutine(GetSpawnPlacementTurnText);

        // 오버레이가 사라진 뒤 타이머 진행
        yield return RunPhaseTimerRoutine(currentPhaseIndex, spawnPlacementDuration, spawnPlacementTimerFill);
        yield return WaitForSpawnPlacementCompleteRoutine();

        currentPhaseIndex = 2;
        // 3단계 화면으로 전환
        ShowOnlyPanel(equipmentSelectionPanel);
        PrepareEquipmentSelection();

        // 3단계는 바로 타이머 진행
        yield return RunPhaseTimerRoutine(currentPhaseIndex, equipmentSelectionDuration, equipmentSelectionTimerFill);

        yield return WaitForBothEquipmentSelectionsRoutine();

        Debug.Log("준비 단계 1~3 완료");
        currentPhaseIndex = -1;
        HideAllPanels();
        flowRoutine = null;
        PrepFlowCompleted?.Invoke();
    }

    private void ShowOnlyPanel(GameObject targetPanel)
    {
        HideAllPanels();

        if (targetPanel != null)
            targetPanel.SetActive(true);
    }

    private IEnumerator ShowTurnIntroRoutine(Func<string> turnTextProvider)
    {
        if (turnIntroCanvasGroup == null)
            yield break;

        UpdateTurnIntroText(turnTextProvider);

        turnIntroCanvasGroup.gameObject.SetActive(true);
        turnIntroCanvasGroup.alpha = 1f;
        turnIntroCanvasGroup.interactable = false;
        turnIntroCanvasGroup.blocksRaycasts = true;

        Graphic introGraphic = turnIntroCanvasGroup.GetComponent<Graphic>();
        if (introGraphic != null)
            introGraphic.raycastTarget = true;

        if (audioSource != null && prepStartSfx != null)
            audioSource.PlayOneShot(prepStartSfx);

        float t = 0f;

        while (t < turnIntroFadeDuration)
        {
            UpdateTurnIntroText(turnTextProvider);
            t += Time.deltaTime;
            turnIntroCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / turnIntroFadeDuration);
            yield return null;
        }

        turnIntroCanvasGroup.alpha = 0f;
        turnIntroCanvasGroup.interactable = false;
        turnIntroCanvasGroup.blocksRaycasts = false;
        turnIntroCanvasGroup.gameObject.SetActive(false);
    }

    private void RefreshObjectPlacementSlotsForRound()
    {
        if (placementManager == null)
            placementManager = FindFirstObjectByType<PlacementManager>();

        if (placementManager != null)
            placementManager.RefreshObjectSlots();
    }

    private void UpdateTurnIntroText(Func<string> turnTextProvider)
    {
        if (turnIntroText == null || turnTextProvider == null)
            return;

        turnIntroText.text = turnTextProvider();
    }

    private string GetObjectPlacementTurnText()
    {
        if (LobbyState.Instance == null)
            return GetFallbackTurnText(objectPlacementPlayerName);

        return CanLocalControlObjectPlacement() ? "My Turn" : "Enemy Turn";
    }

    private string GetSpawnPlacementTurnText()
    {
        if (LobbyState.Instance == null)
            return GetFallbackTurnText(spawnPlacementPlayerName);

        return CanLocalControlSpawnPlacement() ? "My Turn" : "Enemy Turn";
    }

    private string GetFallbackTurnText(string playerName)
    {
#if UNITY_EDITOR
        if (allowEditorLocalTest)
            return "My Turn";
#endif
        return playerName + "'s Turn!";
    }

    private IEnumerator WaitForPrepAuthorityReadyRoutine()
    {
        const float maxWaitSeconds = 2f;
        float elapsed = 0f;

        while (LobbyState.Instance == null && elapsed < maxWaitSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        LobbyState state = LobbyState.Instance;
        while (state != null && state.Runner != null && state.prepRound <= 0 && elapsed < maxWaitSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RunPhaseTimerRoutine(int phaseIndex, float duration, Image timerFill)
    {
        skipRequested = false;
        warningPlayed = false;

        if (timerFill != null)
        {
            timerFill.fillAmount = 1f;
            timerFill.color = Color.white;
        }

        LobbyState state = LobbyState.Instance;
        if (state != null && state.Runner != null)
        {
            state.StartPrepPhaseTimer(phaseIndex, duration);
            yield return WaitForNetworkPhaseTimerReady(state, phaseIndex);

            if (state.IsPrepPhaseTimerReady(phaseIndex))
            {
                while (!skipRequested && !state.IsPrepPhaseTimerExpired(phaseIndex))
                {
                    float ratio = state.GetPrepPhaseTimerRatio(phaseIndex, duration);

                    if (timerFill != null)
                        timerFill.fillAmount = ratio;

                    float remainTime = ratio * duration;

                    if (!warningPlayed && (phaseIndex == 0 || phaseIndex == 1) && remainTime <= 10f)
                    {
                        warningPlayed = true;

                        if (timerFill != null)
                            timerFill.color = Color.red;

                        if (warningAudioSource != null && timerWarningSfx != null)
                            warningAudioSource.PlayOneShot(timerWarningSfx);
                    }

                    yield return null;
                }

                if (warningAudioSource != null)
                    warningAudioSource.Stop();

                if (timerFill != null)
                {
                    timerFill.fillAmount = 0f;
                    timerFill.color = Color.white;
                }

                yield break;
            }
        }

        float remain = duration;

        while (remain > 0f && !skipRequested)
        {
            remain -= Time.deltaTime;

            if (timerFill != null)
                timerFill.fillAmount = Mathf.Clamp01(remain / duration);

            if (!warningPlayed && (phaseIndex == 0 || phaseIndex == 1) && remain <= 10f)
            {
                warningPlayed = true;

                if (timerFill != null)
                    timerFill.color = Color.red;

                if (warningAudioSource != null && timerWarningSfx != null)
                    warningAudioSource.PlayOneShot(timerWarningSfx);
            }

            yield return null;
        }

        if (warningAudioSource != null)
            warningAudioSource.Stop();

        if (timerFill != null)
        {
            timerFill.fillAmount = 0f;
            timerFill.color = Color.white;
        }
    }

    private IEnumerator WaitForNetworkPhaseTimerReady(LobbyState state, int phaseIndex)
    {
        const float maxWaitSeconds = 2f;
        float elapsed = 0f;

        while (!skipRequested && state != null && !state.IsPrepPhaseTimerReady(phaseIndex) && elapsed < maxWaitSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForBothEquipmentSelectionsRoutine()
    {
        if (LobbyState.Instance == null)
            yield break;

        EnsureEquipmentSelection();
        equipmentAllReady = false;
       // LobbyState.Instance.RequestSelectEquipment(selectedEquipmentIndex);
        LobbyState.Instance.RequestEquipmentReady();
        Debug.Log("Equipment selection completed locally. Index=" + selectedEquipmentIndex + ". Waiting for the opponent.");

        while (!equipmentAllReady)
            yield return null;
    }

    public void HideAllPanels()
    {
        if (objectPlacementPanel != null)
            objectPlacementPanel.SetActive(false);

        if (spawnPlacementPanel != null)
            spawnPlacementPanel.SetActive(false);

        if (equipmentSelectionPanel != null)
            equipmentSelectionPanel.SetActive(false);
    }

    public void SkipCurrentPhase()
    {
        if (!CanLocalSkipCurrentPhase())
            return;

        if (!CanFinishCurrentPhase())
            return;

        if (currentPhaseIndex == 2)
            EnsureEquipmentSelection();

        if (currentPhaseIndex == 0 || currentPhaseIndex == 1)
        {
            if (LobbyState.Instance != null)
            {
                LobbyState.Instance.RequestSkipPrepPhase(currentPhaseIndex);
                return;
            }
        }

        skipRequested = true;
    }

    private bool CanLocalSkipCurrentPhase()
    {
        if (currentPhaseIndex == 0)
            return CanLocalControlObjectPlacement();

        if (currentPhaseIndex == 1)
            return CanLocalControlSpawnPlacement();

        return true;
    }

    private bool CanLocalControlObjectPlacement()
    {
        if (LobbyState.Instance != null)
            return LobbyState.Instance.LocalHasObjectPlacementAuthority();

#if UNITY_EDITOR
        return allowEditorLocalTest;
#else
        return false;
#endif
    }

    private bool CanLocalControlSpawnPlacement()
    {
        if (LobbyState.Instance != null)
            return LobbyState.Instance.LocalHasSpawnPlacementAuthority();

#if UNITY_EDITOR
        return allowEditorLocalTest;
#else
        return false;
#endif
    }

    private IEnumerator WaitForObjectPlacementCompleteRoutine()
    {
        yield break;
    }

    private IEnumerator WaitForSpawnPlacementCompleteRoutine()
    {
        if (HasBothSpawnPoints())
            yield break;

        Debug.LogWarning("Spawn placement phase is waiting for both spawn points.");

        while (!HasBothSpawnPoints())
            yield return null;
    }

    private bool CanFinishCurrentPhase()
    {
        if (currentPhaseIndex == 0)
            return true;

        if (currentPhaseIndex == 1)
        {
            if (HasBothSpawnPoints())
                return true;

            Debug.LogWarning("Place both spawn points before finishing spawn placement.");
            return false;
        }

        return true;
    }

    private bool HasPlacedObject()
    {
        return dataStore != null &&
               dataStore.placedObjects != null &&
               dataStore.placedObjects.Count > 0;
    }

    private bool HasBothSpawnPoints()
    {
        return dataStore != null &&
               dataStore.spawnData != null &&
               dataStore.spawnData.hasMySpawn &&
               dataStore.spawnData.hasOpponentSpawn;
    }

    private void HandleNetworkSkipRequested(int phaseIndex)
    {
        if (phaseIndex != currentPhaseIndex)
            return;

        skipRequested = true;
    }

    private void HandleEquipmentAllReady()
    {
        equipmentAllReady = true;
    }

    public WeaponData GetEquipmentByIndex(int equipmentIndex)
    {
        if (equipmentPool == null || equipmentIndex < 0 || equipmentIndex >= equipmentPool.Length)
            return null;

        return equipmentPool[equipmentIndex];
    }

    private void BindEquipmentCards()
    {
        equipmentButtons.Clear();

        //if (equipmentCards == null || equipmentCards.Length == 0)
        //equipmentCards = FindEquipmentCards();
        /*
        for (int i = 0; i < equipmentCards.Length; i++)
        {
            Transform card = equipmentCards[i];
            if (card == null)
                continue;

            Button button = card.GetComponent<Button>();
            if (button == null)
                button = card.gameObject.AddComponent<Button>();

            int index = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectEquipment(index));
            equipmentButtons.Add(button);
        }
        */
        if (equipmentCards == null) return;

        for (int i = 0; i < equipmentCards.Length; i++)
        {
            EquipmentCardUI card = equipmentCards[i];
            if (card == null) continue;

            // card는 EquipmentCardUI 스크립트이므로, 그 스크립트가 붙어있는 gameObject에서 버튼을 찾습니다.
            Button button = card.GetComponent<Button>();
            if (button == null)
                button = card.gameObject.AddComponent<Button>();

            int localIndex = i; // 0, 1, 2
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectEquipment(localIndex));

            equipmentButtons.Add(button);
        }
    }

    private Transform[] FindEquipmentCards()
    {
        if (equipmentSelectionPanel == null)
            return new Transform[0];

        List<Transform> cards = new List<Transform>();
        foreach (Transform child in equipmentSelectionPanel.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.StartsWith("EquipCard_", StringComparison.Ordinal))
                cards.Add(child);
        }

        cards.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return cards.ToArray();
    }

    private void PrepareEquipmentSelection()
    {
        //selectedEquipmentIndex = dataStore != null ? dataStore.selectedEquipmentIndex : -1;
        selectedEquipmentIndex = -1;
        UpdateEquipmentCardLabels();
        RefreshEquipmentCardVisuals();
    }

    private void SelectEquipment(int localIndex)
    {
        if (currentPhaseIndex != 2)
            return;

        //if (equipmentPool == null || index < 0 || index >= equipmentPool.Length || equipmentPool[index] == null)
        //return;

        if (localIndex < 0 || localIndex >= currentRandomPool.Count) return;

        if (SoundManager.instance != null)
            SoundManager.instance.ButtonClick2();

        selectedEquipmentIndex = localIndex;
        WeaponData selectedWeapon = currentRandomPool[localIndex];

        int masterIndex = System.Array.IndexOf(equipmentPool, selectedWeapon);
        if (masterIndex == -1) masterIndex = 0;

        LocalPlayerData.SelectedWeaponMasterIndex = masterIndex;

        if (dataStore != null)
            dataStore.SaveEquipmentSelection(masterIndex);

        RefreshEquipmentCardVisuals();

     }

    private void EnsureEquipmentSelection()
    {
        if (selectedEquipmentIndex >= 0)
            return;

        if (equipmentPool == null || equipmentPool.Length == 0)
        {
            selectedEquipmentIndex = -1;
            return;
        }
        /*
        for (int i = 0; i < equipmentPool.Length; i++)
        {
            if (equipmentPool[i] != null)
            {
                selectedEquipmentIndex = i;

                if (dataStore != null)
                    dataStore.SaveEquipmentSelection(i);

                RefreshEquipmentCardVisuals();
                Debug.Log("Default equipment selected: " + GetEquipmentDisplayName(equipmentPool[i]) + " (" + i + ")");
                return;
            }
        }
        */
        int randomLocalIndex = UnityEngine.Random.Range(0, currentRandomPool.Count);
        SelectEquipment(randomLocalIndex);
    }

    private void UpdateEquipmentCardLabels()
    {
        if (equipmentCards == null)
            return;

        for (int i = 0; i < equipmentCards.Length; i++)
        {
            // if (equipmentCards[i] == null)
            //continue;
            EquipmentCardUI card = equipmentCards[i];
            //if (equipmentCards[i] == null || i >= currentRandomPool.Count) continue;
            if (card == null || i >= currentRandomPool.Count) continue;

            WeaponData data = currentRandomPool[i];
            card.SetWeaponData(data);

            // 1. 이름 적용
            if (card.nameText != null)
                card.nameText.text = data != null ? data.weaponName : "Empty";

            // 2. 설명 적용
            if (card.descriptionText != null)
                card.descriptionText.text = data != null ? data.weaponDescription : "";

            // 3. 아이콘 이미지 적용
            if (card.weaponIconImage != null)
            {
                if (data != null && data.weaponIcon != null)
                {
                    card.weaponIconImage.sprite = data.weaponIcon;
                    card.weaponIconImage.enabled = true; // 사진 켜기
                }
                else
                {
                    card.weaponIconImage.enabled = false; // 사진 없으면 깔끔하게 숨기기
                }
            }

            /*
            Text label = equipmentCards[i].GetComponentInChildren<Text>(true);
            if (label == null)
                continue;

            WeaponData data = GetEquipmentByIndex(i);
            label.text = data != null ? GetEquipmentDisplayName(data) : "Empty";
            */
        }
    }

    private void RefreshEquipmentCardVisuals()
    {
        if (equipmentCards == null)
            return;

        for (int i = 0; i < equipmentCards.Length; i++)
        {
            EquipmentCardUI card = equipmentCards[i];
            if (card == null) continue;
            /*
            if (equipmentCards[i] == null)
                continue;

            Image image = equipmentCards[i].GetComponent<Image>();
            if (image != null)
                image.color = i == selectedEquipmentIndex ? equipmentCardSelectedColor : equipmentCardNormalColor;
            */
            Image image = card.GetComponent<Image>();
            if (image != null)
            {
                image.color = (i == selectedEquipmentIndex) ? equipmentCardSelectedColor : equipmentCardNormalColor;
            }
        }
    }

    private string GetEquipmentDisplayName(WeaponData data)
    {
        if (data == null)
            return "Empty";

        return string.IsNullOrEmpty(data.weaponName) ? data.name : data.weaponName;
    }
}
