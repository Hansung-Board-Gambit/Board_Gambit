using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrepPhaseFlowUI : MonoBehaviour
{
    public event Action PrepFlowCompleted;

    [Header("Flow")]
    public bool playOnStart = true;

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
    public Transform[] equipmentCards;
    public Color equipmentCardNormalColor = new Color(0f, 0f, 0f, 0.392f);
    public Color equipmentCardSelectedColor = new Color(0.15f, 0.45f, 0.85f, 0.75f);

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

    public void BeginFlow()
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

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

        // 그 위에 턴 소개 오버레이
        yield return ShowTurnIntroRoutine(objectPlacementPlayerName);

        // 오버레이가 사라진 뒤 타이머 진행
        yield return RunPhaseTimerRoutine(
            objectPlacementDuration,
            objectPlacementTimerFill
        );

        currentPhaseIndex = 1;
        // 2단계 화면 먼저 띄우기
        ShowOnlyPanel(spawnPlacementPanel);

        // 그 위에 턴 소개 오버레이
        yield return ShowTurnIntroRoutine(spawnPlacementPlayerName);

        // 오버레이가 사라진 뒤 타이머 진행
        yield return RunPhaseTimerRoutine(
            spawnPlacementDuration,
            spawnPlacementTimerFill
        );

        currentPhaseIndex = 2;
        // 3단계 화면으로 전환
        ShowOnlyPanel(equipmentSelectionPanel);
        PrepareEquipmentSelection();

        // 3단계는 바로 타이머 진행
        yield return RunPhaseTimerRoutine(
            equipmentSelectionDuration,
            equipmentSelectionTimerFill
        );

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

    private IEnumerator ShowTurnIntroRoutine(string playerName)
    {
        if (turnIntroCanvasGroup == null)
            yield break;

        if (turnIntroText != null)
            turnIntroText.text = playerName + "'s Turn!";

        turnIntroCanvasGroup.gameObject.SetActive(true);
        turnIntroCanvasGroup.alpha = 1f;

        float t = 0f;

        while (t < turnIntroFadeDuration)
        {
            t += Time.deltaTime;
            turnIntroCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / turnIntroFadeDuration);
            yield return null;
        }

        turnIntroCanvasGroup.alpha = 0f;
        turnIntroCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator RunPhaseTimerRoutine(float duration, Image timerFill)
    {
        skipRequested = false;

        if (timerFill != null)
            timerFill.fillAmount = 1f;

        float remain = duration;

        while (remain > 0f && !skipRequested)
        {
            remain -= Time.deltaTime;

            if (timerFill != null)
                timerFill.fillAmount = Mathf.Clamp01(remain / duration);

            yield return null;
        }

        if (timerFill != null)
            timerFill.fillAmount = 0f;
    }

    private IEnumerator WaitForBothEquipmentSelectionsRoutine()
    {
        if (LobbyState.Instance == null)
            yield break;

        EnsureEquipmentSelection();
        equipmentAllReady = false;
        LobbyState.Instance.RequestSelectEquipment(selectedEquipmentIndex);
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
            return LobbyState.Instance != null && LobbyState.Instance.LocalHasObjectPlacementAuthority();

        if (currentPhaseIndex == 1)
            return LobbyState.Instance != null && LobbyState.Instance.LocalHasSpawnPlacementAuthority();

        return true;
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

        if (equipmentCards == null || equipmentCards.Length == 0)
            equipmentCards = FindEquipmentCards();

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
        selectedEquipmentIndex = dataStore != null ? dataStore.selectedEquipmentIndex : -1;
        UpdateEquipmentCardLabels();
        RefreshEquipmentCardVisuals();
    }

    private void SelectEquipment(int index)
    {
        if (currentPhaseIndex != 2)
            return;

        if (equipmentPool == null || index < 0 || index >= equipmentPool.Length || equipmentPool[index] == null)
            return;

        selectedEquipmentIndex = index;

        if (dataStore != null)
            dataStore.SaveEquipmentSelection(index);

        RefreshEquipmentCardVisuals();
        Debug.Log("Selected equipment: " + GetEquipmentDisplayName(equipmentPool[index]) + " (" + index + ")");
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
    }

    private void UpdateEquipmentCardLabels()
    {
        if (equipmentCards == null)
            return;

        for (int i = 0; i < equipmentCards.Length; i++)
        {
            if (equipmentCards[i] == null)
                continue;

            Text label = equipmentCards[i].GetComponentInChildren<Text>(true);
            if (label == null)
                continue;

            WeaponData data = GetEquipmentByIndex(i);
            label.text = data != null ? GetEquipmentDisplayName(data) : "Empty";
        }
    }

    private void RefreshEquipmentCardVisuals()
    {
        if (equipmentCards == null)
            return;

        for (int i = 0; i < equipmentCards.Length; i++)
        {
            if (equipmentCards[i] == null)
                continue;

            Image image = equipmentCards[i].GetComponent<Image>();
            if (image != null)
                image.color = i == selectedEquipmentIndex ? equipmentCardSelectedColor : equipmentCardNormalColor;
        }
    }

    private string GetEquipmentDisplayName(WeaponData data)
    {
        if (data == null)
            return "Empty";

        return string.IsNullOrEmpty(data.weaponName) ? data.name : data.weaponName;
    }
}
