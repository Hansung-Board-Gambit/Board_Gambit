using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PrepPhaseFlowUI : MonoBehaviour
{
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

    private bool skipRequested;

    private void Awake()
    {
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
        StartCoroutine(MainFlow());
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
    }

    private IEnumerator MainFlow()
    {
        // 1단계 화면 먼저 띄우기
        ShowOnlyPanel(objectPlacementPanel);

        // 그 위에 턴 소개 오버레이
        yield return ShowTurnIntroRoutine(objectPlacementPlayerName);

        // 오버레이가 사라진 뒤 타이머 진행
        yield return RunPhaseTimerRoutine(
            objectPlacementDuration,
            objectPlacementTimerFill
        );

        // 2단계 화면 먼저 띄우기
        ShowOnlyPanel(spawnPlacementPanel);

        // 그 위에 턴 소개 오버레이
        yield return ShowTurnIntroRoutine(spawnPlacementPlayerName);

        // 오버레이가 사라진 뒤 타이머 진행
        yield return RunPhaseTimerRoutine(
            spawnPlacementDuration,
            spawnPlacementTimerFill
        );

        // 3단계 화면으로 전환
        ShowOnlyPanel(equipmentSelectionPanel);

        // 3단계는 바로 타이머 진행
        yield return RunPhaseTimerRoutine(
            equipmentSelectionDuration,
            equipmentSelectionTimerFill
        );

        Debug.Log("준비 단계 1~3 완료");
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

    private void HideAllPanels()
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
        skipRequested = true;
    }
}