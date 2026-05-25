using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GameSceneFrameBuilder
{
    private static Font _font;

    [MenuItem("Tools/BoardGambit/Rebuild GameScene Frame")]
    public static void RebuildGameSceneFrame()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        EnsureEventSystem();

        GameObject managers = CreateRoot("Managers");
        GameObject world = CreateRoot("World");
        GameObject canvas = EnsureCanvas();

        GameObject gameFlow = CreateChild(managers, "GameFlow");
        CreateChild(managers, "BoardManager");
        CreateChild(managers, "RoundManager");
        CreateChild(managers, "PlayerManager");

        CreateChild(world, "BoardRoot");
        CreateChild(world, "StructureRoot");
        CreateChild(world, "SpawnPointRoot");
        CreateChild(world, "Players");

        GameObject prepUI = CreateUIRoot(canvas, "PrepUI");
        GameObject overlayUI = CreateUIRoot(canvas, "OverlayUI");
        GameObject hudUI = CreateUIRoot(canvas, "HUDUI");

        StretchFull(prepUI);
        StretchFull(overlayUI);
        StretchFull(hudUI);

        GameObject objectPanel = CreatePanel(prepUI, "Prep_ObjectPlacementPanel", new Color(0, 0, 0, 0));
        GameObject spawnPanel = CreatePanel(prepUI, "Prep_SpawnPlacementPanel", new Color(0, 0, 0, 0));
        GameObject equipPanel = CreatePanel(prepUI, "Prep_RandomEquipPanel", new Color(0, 0, 0, 0));

        BuildObjectPlacementPanel(objectPanel);
        BuildSpawnPlacementPanel(spawnPanel);
        BuildEquipmentPanel(equipPanel);

        GameObject turnIntro = CreatePanel(overlayUI, "Overlay_TurnIntro", new Color(0, 0, 0, 0.35f));
        BuildTurnIntro(turnIntro);

        GameObject hud = CreatePanel(hudUI, "HUD_InGame", new Color(0, 0, 0, 0));
        hud.SetActive(false);

        objectPanel.SetActive(false);
        spawnPanel.SetActive(false);
        equipPanel.SetActive(false);

        AttachPrepFlowScript(
            gameFlow,
            turnIntro,
            objectPanel,
            spawnPanel,
            equipPanel
        );

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = gameFlow;

        Debug.Log("GameScene 틀 재생성 완료");
    }

    private static void AttachPrepFlowScript(
        GameObject gameFlow,
        GameObject turnIntro,
        GameObject objectPanel,
        GameObject spawnPanel,
        GameObject equipPanel)
    {
        var script = gameFlow.GetComponent<PrepPhaseFlowUI>();
        if (script == null)
            script = Undo.AddComponent<PrepPhaseFlowUI>(gameFlow);

        CanvasGroup turnIntroCanvasGroup = turnIntro.GetComponent<CanvasGroup>();
        if (turnIntroCanvasGroup == null)
            turnIntroCanvasGroup = Undo.AddComponent<CanvasGroup>(turnIntro);

        turnIntroCanvasGroup.interactable = false;
        turnIntroCanvasGroup.blocksRaycasts = false;
        turnIntroCanvasGroup.alpha = 0f;

        script.turnIntroCanvasGroup = turnIntroCanvasGroup;
        script.turnIntroText = FindText(turnIntro.transform, "TurnIntroText");

        script.objectPlacementPanel = objectPanel;
        script.spawnPlacementPanel = spawnPanel;
        script.equipmentSelectionPanel = equipPanel;

        script.objectPlacementTimerFill = FindImage(objectPanel.transform, "TimerFill");
        script.spawnPlacementTimerFill = FindImage(spawnPanel.transform, "TimerFill");
        script.equipmentSelectionTimerFill = FindImage(equipPanel.transform, "TimerFill");

        script.objectPlacementFinishButton = FindButton(objectPanel.transform, "FinishButton");
        script.spawnPlacementFinishButton = FindButton(spawnPanel.transform, "FinishButton");
        script.equipmentSelectionFinishButton = FindButton(equipPanel.transform, "FinishButton");
    }

    private static Text FindText(Transform root, string name)
    {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<Text>() : null;
    }

    private static Image FindImage(Transform root, string name)
    {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static Button FindButton(Transform root, string name)
    {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeep(root.GetChild(i), name);
            if (result != null)
                return result;
        }

        return null;
    }

    private static GameObject CreateRoot(string name)
    {
        GameObject found = GameObject.Find(name);
        if (found != null)
            return found;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        Transform found = parent.transform.Find(name);
        if (found != null)
            return found.gameObject;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static GameObject CreateUIRoot(GameObject parent, string name)
    {
        Transform found = parent.transform.Find(name);
        if (found != null)
            return found.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static GameObject EnsureCanvas()
    {
        Canvas found = Object.FindFirstObjectByType<Canvas>();
        if (found != null)
        {
            ConfigureCanvas(found.gameObject);
            return found.gameObject;
        }

        GameObject go = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        ConfigureCanvas(go);
        return go;
    }

    private static void ConfigureCanvas(GameObject go)
    {
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule)
        );

        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    private static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        Transform found = parent.transform.Find(name);
        if (found != null)
            return found.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent.transform, false);

        Image img = go.GetComponent<Image>();
        img.color = color;

        StretchFull(go);
        return go;
    }

    private static GameObject CreateBox(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static GameObject CreateText(GameObject parent, string name, string text, int fontSize, TextAnchor anchor, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent.transform, false);

        Text txt = go.GetComponent<Text>();
        txt.text = text;
        txt.font = _font;
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.color = color;
        txt.raycastTarget = false;

        return go;
    }

    private static GameObject CreateButton(GameObject parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent.transform, false);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.21f, 0.26f, 0.36f, 0.95f);

        GameObject text = CreateText(go, "Text", label, 22, TextAnchor.MiddleCenter, Color.white);
        StretchFull(text);

        return go;
    }

    private static GameObject CreateTimerBar(GameObject parent, string name)
    {
        GameObject timerBar = CreateBox(parent, name, new Color(0.08f, 0.08f, 0.08f, 0.95f));
        GameObject fill = new GameObject("TimerFill", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(fill, "Create TimerFill");
        fill.transform.SetParent(timerBar.transform, false);

        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.93f, 0.93f, 0.93f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillAmount = 1f;

        StretchFull(fill);
        return timerBar;
    }

    private static void StretchFull(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;
    }

    private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;
    }

    private static void BuildTurnIntro(GameObject root)
    {
        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = Undo.AddComponent<CanvasGroup>(root);

        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.alpha = 0f;

        GameObject text = CreateText(root, "TurnIntroText", "ExPlayer's Turn!", 52, TextAnchor.MiddleCenter, Color.white);
        SetRect(text, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-340f, -70f), new Vector2(340f, 70f));
    }

    private static void BuildObjectPlacementPanel(GameObject root)
    {
        GameObject leftPanel = CreateBox(root, "LeftObjectPanel", new Color(0.12f, 0.12f, 0.15f, 0.95f));
        SetRect(leftPanel, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(20f, 110f), new Vector2(230f, -110f));

        for (int i = 0; i < 5; i++)
        {
            GameObject slot = CreateBox(leftPanel, $"ObjectSlot_{i + 1}", new Color(0.22f, 0.22f, 0.25f, 1f));
            float top = -20f - (i * 120f);
            SetRect(slot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(15f, top - 100f), new Vector2(-15f, top));

            GameObject label = CreateText(slot, "Label", "Random Object", 20, TextAnchor.MiddleCenter, Color.white);
            StretchFull(label);
        }

        GameObject boardArea = CreateBox(root, "BoardArea", new Color(0.18f, 0.18f, 0.18f, 1f));
        SetRect(boardArea, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(260f, 110f), new Vector2(-260f, -110f));

        GameObject boardLabel = CreateText(boardArea, "BoardLabel", "1. Object Placement", 34, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f));
        StretchFull(boardLabel);

        GameObject rightPanel = CreateBox(root, "RightInfoPanel", new Color(0.12f, 0.12f, 0.15f, 0.95f));
        SetRect(rightPanel, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-230f, 110f), new Vector2(-20f, -110f));

        GameObject timerBar = CreateTimerBar(rightPanel, "TimerBar");
        SetRect(timerBar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(15f, -220f), new Vector2(45f, 220f));

        GameObject selectLabel = CreateText(rightPanel, "SelectLabel", "Select", 22, TextAnchor.MiddleLeft, Color.white);
        SetRect(selectLabel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -70f), new Vector2(-15f, -35f));

        GameObject moveLabel = CreateText(rightPanel, "MoveLabel", "Move", 22, TextAnchor.MiddleLeft, Color.white);
        SetRect(moveLabel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -115f), new Vector2(-15f, -80f));

        GameObject rotateLabel = CreateText(rightPanel, "RotateLabel", "Rotate", 22, TextAnchor.MiddleLeft, Color.white);
        SetRect(rotateLabel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -160f), new Vector2(-15f, -125f));

        GameObject deleteLabel = CreateText(rightPanel, "DeleteLabel", "Delete", 22, TextAnchor.MiddleLeft, Color.white);
        SetRect(deleteLabel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -205f), new Vector2(-15f, -170f));

        GameObject pointPanel = CreateBox(rightPanel, "PointPanel", new Color(0.09f, 0.28f, 0.20f, 0.95f));
        SetRect(pointPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(15f, 95f), new Vector2(-15f, 170f));

        GameObject pointText = CreateText(pointPanel, "PointText", "Available Object Point : 5", 20, TextAnchor.MiddleCenter, Color.white);
        StretchFull(pointText);

        GameObject finishButton = CreateButton(rightPanel, "FinishButton", "Finish");
        SetRect(finishButton, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(15f, 15f), new Vector2(-15f, 75f));

        GameObject guidePanel = CreateBox(root, "GuidePanel", new Color(0.10f, 0.10f, 0.10f, 0.85f));
        SetRect(guidePanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-220f, 20f), new Vector2(220f, 70f));

        GameObject guideText = CreateText(guidePanel, "GuideText", "조작 설명 영역", 22, TextAnchor.MiddleCenter, Color.white);
        StretchFull(guideText);
    }

    private static void BuildSpawnPlacementPanel(GameObject root)
    {
        GameObject boardArea = CreateBox(root, "BoardArea", new Color(0.18f, 0.18f, 0.18f, 1f));
        SetRect(boardArea, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 110f), new Vector2(-260f, -110f));

        GameObject boardLabel = CreateText(boardArea, "BoardLabel", "2. Spawn Placement", 34, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f));
        StretchFull(boardLabel);

        GameObject rightPanel = CreateBox(root, "RightInfoPanel", new Color(0.12f, 0.12f, 0.15f, 0.95f));
        SetRect(rightPanel, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-230f, 110f), new Vector2(-20f, -110f));

        GameObject timerBar = CreateTimerBar(rightPanel, "TimerBar");
        SetRect(timerBar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(15f, -220f), new Vector2(45f, 220f));

        GameObject mySpawn = CreateText(rightPanel, "SetMySpawnLabel", "Set My Spawn", 24, TextAnchor.MiddleLeft, Color.white);
        SetRect(mySpawn, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -90f), new Vector2(-15f, -45f));

        GameObject oppSpawn = CreateText(rightPanel, "SetOpponentSpawnLabel", "Set Opponent Spawn", 24, TextAnchor.MiddleLeft, Color.white);
        SetRect(oppSpawn, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -150f), new Vector2(-15f, -105f));

        GameObject finishButton = CreateButton(rightPanel, "FinishButton", "Finish");
        SetRect(finishButton, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(15f, 15f), new Vector2(-15f, 75f));
    }

    private static void BuildEquipmentPanel(GameObject root)
    {
        GameObject topLeft = CreateButton(root, "SkipButton", "Skip");
        SetRect(topLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -85f), new Vector2(180f, -25f));

        GameObject descriptionPanel = CreateBox(root, "DescriptionPanel", new Color(0.12f, 0.12f, 0.15f, 0.95f));
        SetRect(descriptionPanel, new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(30f, 110f), new Vector2(-15f, -110f));

        GameObject descriptionText = CreateText(descriptionPanel, "DescriptionText", "3. Equipment Selection", 30, TextAnchor.MiddleCenter, Color.white);
        StretchFull(descriptionText);

        GameObject scrollArea = CreateBox(root, "ScrollArea", new Color(0.18f, 0.18f, 0.18f, 0.95f));
        SetRect(scrollArea, new Vector2(0.48f, 0f), new Vector2(0.78f, 1f), new Vector2(15f, 110f), new Vector2(-15f, -110f));

        GameObject scrollText = CreateText(scrollArea, "ScrollText", "설명창 스크롤 영역", 24, TextAnchor.MiddleCenter, Color.white);
        StretchFull(scrollText);

        GameObject rightPanel = CreateBox(root, "RightInfoPanel", new Color(0.12f, 0.12f, 0.15f, 0.95f));
        SetRect(rightPanel, new Vector2(0.78f, 0f), new Vector2(1f, 1f), new Vector2(15f, 110f), new Vector2(-30f, -110f));

        GameObject timerBar = CreateTimerBar(rightPanel, "TimerBar");
        SetRect(timerBar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(15f, -220f), new Vector2(45f, 220f));

        for (int i = 0; i < 3; i++)
        {
            GameObject selectButton = CreateButton(rightPanel, $"SelectButton_{i + 1}", "Select");
            float top = -80f - (i * 95f);
            SetRect(selectButton, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, top - 60f), new Vector2(-15f, top));
        }

        GameObject finishButton = CreateButton(rightPanel, "FinishButton", "Finish");
        SetRect(finishButton, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(15f, 15f), new Vector2(-15f, 75f));
    }
}
