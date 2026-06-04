using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpawnPlacementManager : MonoBehaviour
{
    public enum SpawnMode
    {
        None,
        MySpawn,
        OpponentSpawn
    }

    [Header("References")]
    public Camera mainCamera;
    public GameObject spawnPlacementPanel;
    public Collider boardCollider;
    public PrepDataStore dataStore;

    [Header("Markers")]
    public GameObject mySpawnMarker;
    public GameObject opponentSpawnMarker;
    public GameObject mySpawnMarkerPrefab;
    public GameObject opponentSpawnMarkerPrefab;

    [Header("Button Labels")]
    public Text mySpawnButtonLabel;
    public Text opponentSpawnButtonLabel;
    public string setMySpawnText = "Set My Spawn";
    public string setEnemySpawnText = "Set Enemy Spawn";
    public string mySpawnDisplayText = "My Spawn";
    public string enemySpawnDisplayText = "Enemy Spawn";
    public float spawnButtonMinHeight = 56f;
    public float spawnButtonHorizontalPadding = 12f;
    public Color spawnButtonBackgroundColor = new Color(0.08f, 0.09f, 0.13f, 0.95f);
    public Color spawnButtonSelectedColor = new Color(0.12f, 0.32f, 0.58f, 0.95f);
    public Color mySpawnButtonSelectedColor = new Color(0.4196079f, 1f, 0.8745098f, 0.95f);
    public Color enemySpawnButtonSelectedColor = new Color(0.682353f, 0.4196079f, 1f, 0.95f);

    [Header("Layers")]
    public LayerMask boardLayer;
    public LayerMask placedObjectMask;

    [Header("Settings")]
    public float gridSize = 1f;
    public int blockedEdgeCellCount = 0;
    public bool useCircularBoardBounds = false;
    public float markerYOffset = 0.3f;
    public bool fitSpawnMarkersToGridCell = true;
    public float spawnMarkerCellFill = 0.9f;
    public Vector3 spawnCheckHalfExtents = new Vector3(0.4f, 1f, 0.4f);

    [Header("Spawn Shadows")]
    public bool hideShadowsDuringSpawnPlacement = true;
    public Light[] spawnPlacementShadowLights;

    [Header("Editor Test")]
    public bool allowEditorLocalTest = false;

    [Header("Sound")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip selectSfx;     // 버튼 전환
    [SerializeField] private AudioClip placeSfx;      // 스폰 배치 성공
    [SerializeField] private AudioClip errorSfx;      // 배치 불가

    private SpawnMode currentMode = SpawnMode.None;
    private GameObject previewMarker;
    private SpawnMode previewMarkerMode = SpawnMode.None;
    private Image mySpawnButtonBackground;
    private Image opponentSpawnButtonBackground;
    private GameObject activeMySpawnMarkerPrefab;
    private GameObject activeOpponentSpawnMarkerPrefab;

    private bool spawnPlacementShadowsHidden;
    private readonly Dictionary<Light, LightShadows> originalSpawnLightShadows = new Dictionary<Light, LightShadows>();

    private void OnEnable()
    {
        LobbyState.PrepSpawnPlaced += HandleNetworkSpawnPlaced;
    }

    private void OnDisable()
    {
        LobbyState.PrepSpawnPlaced -= HandleNetworkSpawnPlaced;
        DestroyPreviewMarker();
        RestoreSpawnPlacementShadows();
    }

    private void OnDestroy()
    {
        RestoreSpawnPlacementShadows();
    }

    private void Start()
    {
        EnsureSpawnButtonsCanReceiveClicks();
        CacheSpawnButtonLabels();
        UpdateSpawnButtonLabels();
        SetupSpawnButtonBackgrounds();
        UpdateSpawnButtonVisuals();
        EnsurePrefabSpawnMarkers();
        DisableMarkerColliders();
        RestoreSavedMarkers();
    }

    private void Update()
    {
        bool spawnPlacementActive = spawnPlacementPanel != null && spawnPlacementPanel.activeInHierarchy;
        SetSpawnPlacementShadowsHidden(spawnPlacementActive && hideShadowsDuringSpawnPlacement);

        if (!spawnPlacementActive)
        {
            SetPreviewMarkerActive(false);
            return;
        }

        UpdateSpawnButtonLabels();
        UpdateSpawnButtonVisuals();
        if (EnsurePrefabSpawnMarkers())
        {
            DisableMarkerColliders();
            RestoreSavedMarkers();
            DestroyPreviewMarker();
        }

        if (!CanLocalControlSpawnPlacement())
        {
            SetPreviewMarkerActive(false);
            return;
        }

        if (currentMode == SpawnMode.None)
        {
            SetPreviewMarkerActive(false);
            return;
        }

        if (IsPointerBlockedByUi())
        {
            SetPreviewMarkerActive(false);
            return;
        }

        Vector3 snappedPosition;
        if (!TryGetSnappedBoardPoint(out snappedPosition))
        {
            SetPreviewMarkerActive(false);
            return;
        }

        if (!CanPlaceSpawn(snappedPosition))
        {
            if (Input.GetMouseButtonDown(0))
                PlaySfx(errorSfx);

            SetPreviewMarkerActive(false);
            return;
        }

        UpdatePreviewMarker(snappedPosition);

        if (!Input.GetMouseButtonDown(0))
            return;

        if (currentMode == SpawnMode.MySpawn)
        {
            PlaySfx(placeSfx);
            RequestSpawnPlacement(true, snappedPosition);
        }
        else if (currentMode == SpawnMode.OpponentSpawn)
        {
            PlaySfx(placeSfx);
            RequestSpawnPlacement(false, snappedPosition);
        }

        SetPreviewMarkerActive(false);
    }

    public void SetMySpawnMode()
    {
        if (!CanLocalControlSpawnPlacement())
            return;

        if (currentMode != SpawnMode.MySpawn)
            PlaySfx(selectSfx);

        currentMode = SpawnMode.MySpawn;
        UpdateSpawnButtonVisuals();
    }

    public void SetOpponentSpawnMode()
    {
        if (!CanLocalControlSpawnPlacement())
            return;

        if (currentMode != SpawnMode.OpponentSpawn)
            PlaySfx(selectSfx);

        currentMode = SpawnMode.OpponentSpawn;
        UpdateSpawnButtonVisuals();
    }

    public void ClearMode()
    {
        currentMode = SpawnMode.None;
        SetPreviewMarkerActive(false);
        UpdateSpawnButtonVisuals();
    }

    private void RequestSpawnPlacement(bool isMySpawn, Vector3 position)
    {
        if (LobbyState.Instance != null)
        {
            LobbyState.Instance.RequestPlacePrepSpawn(isMySpawn, position);
            return;
        }

        ApplySpawnPlacement(isMySpawn, position);
    }

    private void HandleNetworkSpawnPlaced(bool isMySpawn, Vector3 position)
    {
        ApplySpawnPlacement(isMySpawn, position);
    }

    private void ApplySpawnPlacement(bool isMySpawn, Vector3 position)
    {
        if (dataStore == null)
            return;

        if (isMySpawn)
        {
            dataStore.SaveMySpawn(position);
            UpdateMarker(mySpawnMarker, position);
        }
        else
        {
            dataStore.SaveOpponentSpawn(position);
            UpdateMarker(opponentSpawnMarker, position);
        }
    }

    private bool TryGetSnappedBoardPoint(out Vector3 snappedPosition)
    {
        snappedPosition = Vector3.zero;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!TryRaycastBoard(ray, out hit))
            return false;

        float safeGridSize = Mathf.Max(0.01f, Mathf.Abs(gridSize));
        snappedPosition = hit.point;

        if (boardCollider != null)
        {
            Bounds bounds = boardCollider.bounds;
            snappedPosition.x = SnapToBoardCellCenter(snappedPosition.x, bounds.min.x, bounds.max.x, safeGridSize);
            snappedPosition.z = SnapToBoardCellCenter(snappedPosition.z, bounds.min.z, bounds.max.z, safeGridSize);
        }
        else
        {
            snappedPosition.x = Mathf.Round(snappedPosition.x / safeGridSize) * safeGridSize;
            snappedPosition.z = Mathf.Round(snappedPosition.z / safeGridSize) * safeGridSize;
        }

        snappedPosition.y = GetBoardTopY() + markerYOffset;

        return true;
    }

    private bool CanPlaceSpawn(Vector3 position)
    {
        if (boardCollider == null)
            return false;

        Bounds bounds = boardCollider.bounds;
        float edgeMargin = GetBlockedEdgeMargin();
        bool insideBoard =
            position.x - spawnCheckHalfExtents.x >= bounds.min.x + edgeMargin &&
            position.x + spawnCheckHalfExtents.x <= bounds.max.x - edgeMargin &&
            position.z - spawnCheckHalfExtents.z >= bounds.min.z + edgeMargin &&
            position.z + spawnCheckHalfExtents.z <= bounds.max.z - edgeMargin;

        if (!insideBoard)
            return false;

        if (useCircularBoardBounds && !IsSpawnInsideCircularBoard(position, bounds, edgeMargin))
            return false;

        Collider[] overlaps = Physics.OverlapBox(
            new Vector3(position.x, bounds.center.y + 0.5f, position.z),
            spawnCheckHalfExtents,
            Quaternion.identity,
            placedObjectMask
        );

        return overlaps.Length == 0;
    }

    private void UpdateMarker(GameObject marker, Vector3 position)
    {
        if (marker == null)
            return;

        marker.SetActive(true);
        marker.transform.position = position;
        SetCollidersEnabled(marker, false);
    }

    private void UpdatePreviewMarker(Vector3 position)
    {
        GameObject sourceMarker = GetCurrentModeMarker();
        if (sourceMarker == null)
        {
            SetPreviewMarkerActive(false);
            return;
        }

        EnsurePreviewMarker(sourceMarker);
        if (previewMarker == null)
            return;

        previewMarker.transform.position = position;
        previewMarker.transform.rotation = sourceMarker.transform.rotation;
        previewMarker.transform.localScale = sourceMarker.transform.localScale;
        previewMarker.SetActive(true);
        SetCollidersEnabled(previewMarker, false);
    }

    private void EnsurePreviewMarker(GameObject sourceMarker)
    {
        if (previewMarker != null && previewMarkerMode == currentMode)
            return;

        DestroyPreviewMarker();

        previewMarker = Instantiate(sourceMarker, sourceMarker.transform.parent);
        previewMarker.name = sourceMarker.name + "_Preview";
        previewMarkerMode = currentMode;
        previewMarker.SetActive(false);
        SetCollidersEnabled(previewMarker, false);
        SetLayerRecursively(previewMarker, LayerMask.NameToLayer("Ignore Raycast"));
    }

    private GameObject GetCurrentModeMarker()
    {
        if (currentMode == SpawnMode.MySpawn)
            return mySpawnMarker;

        if (currentMode == SpawnMode.OpponentSpawn)
            return opponentSpawnMarker;

        return null;
    }

    private void SetPreviewMarkerActive(bool active)
    {
        if (previewMarker != null)
            previewMarker.SetActive(active);
    }

    private void DestroyPreviewMarker()
    {
        if (previewMarker == null)
            return;

        Destroy(previewMarker);
        previewMarker = null;
        previewMarkerMode = SpawnMode.None;
    }

    public void SetMarkersVisible(bool visible)
    {
        if (mySpawnMarker != null)
            mySpawnMarker.SetActive(visible && dataStore != null && dataStore.spawnData != null && dataStore.spawnData.hasMySpawn);

        if (opponentSpawnMarker != null)
            opponentSpawnMarker.SetActive(visible && dataStore != null && dataStore.spawnData != null && dataStore.spawnData.hasOpponentSpawn);
    }

    private bool TryRaycastBoard(Ray ray, out RaycastHit hit)
    {
        if (boardCollider != null && boardCollider.Raycast(ray, out hit, 500f))
            return true;

        if (boardLayer.value != 0 && Physics.Raycast(ray, out hit, 500f, boardLayer, QueryTriggerInteraction.Ignore))
            return true;

        hit = new RaycastHit();
        return false;
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

    private float GetBoardTopY()
    {
        return boardCollider != null ? boardCollider.bounds.max.y : 0f;
    }

    private float SnapToBoardCellCenter(float value, float min, float max, float safeGridSize)
    {
        float firstLine = Mathf.Ceil(min / safeGridSize) * safeGridSize;
        float lastLine = Mathf.Floor(max / safeGridSize) * safeGridSize;
        int cellCount = Mathf.Max(1, Mathf.RoundToInt((lastLine - firstLine) / safeGridSize));
        int cellIndex = Mathf.FloorToInt((value - firstLine) / safeGridSize);
        cellIndex = Mathf.Clamp(cellIndex, 0, cellCount - 1);
        return firstLine + (cellIndex + 0.5f) * safeGridSize;
    }

    private float GetBlockedEdgeMargin()
    {
        float safeGridSize = Mathf.Max(0.01f, Mathf.Abs(gridSize));
        return Mathf.Max(0, blockedEdgeCellCount) * safeGridSize;
    }

    private bool IsSpawnInsideCircularBoard(Vector3 position, Bounds bounds, float edgeMargin)
    {
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) - edgeMargin;
        if (radius <= 0f)
            return false;

        Vector2 center = new Vector2(bounds.center.x, bounds.center.z);
        float sqrRadius = radius * radius;

        return IsPointInsideCircle(new Vector2(position.x - spawnCheckHalfExtents.x, position.z - spawnCheckHalfExtents.z), center, sqrRadius) &&
               IsPointInsideCircle(new Vector2(position.x - spawnCheckHalfExtents.x, position.z + spawnCheckHalfExtents.z), center, sqrRadius) &&
               IsPointInsideCircle(new Vector2(position.x + spawnCheckHalfExtents.x, position.z - spawnCheckHalfExtents.z), center, sqrRadius) &&
               IsPointInsideCircle(new Vector2(position.x + spawnCheckHalfExtents.x, position.z + spawnCheckHalfExtents.z), center, sqrRadius);
    }

    private bool IsPointInsideCircle(Vector2 point, Vector2 center, float sqrRadius)
    {
        return (point - center).sqrMagnitude <= sqrRadius + 0.001f;
    }

    private bool IsPointerBlockedByUi()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            GameObject hitObject = results[i].gameObject;
            if (hitObject == null)
                continue;

            if (hitObject.name == "BoardArea")
                continue;

            if (hitObject.GetComponentInParent<Selectable>() != null)
                return true;

            Graphic graphic = hitObject.GetComponent<Graphic>();
            if (graphic != null && graphic.raycastTarget && graphic.color.a > 0.01f)
                return true;
        }

        return false;
    }

    private void EnsureSpawnButtonsCanReceiveClicks()
    {
        if (spawnPlacementPanel == null)
            return;

        Button[] buttons = spawnPlacementPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Graphic targetGraphic = buttons[i].targetGraphic;
            if (targetGraphic == null)
            {
                targetGraphic = buttons[i].GetComponent<Graphic>();
                buttons[i].targetGraphic = targetGraphic;
            }

            if (targetGraphic != null)
                targetGraphic.raycastTarget = true;
        }
    }

    private void CacheSpawnButtonLabels()
    {
        if (spawnPlacementPanel == null)
            return;

        if (mySpawnButtonLabel == null)
            mySpawnButtonLabel = FindChildText("SetMySpawnLabel");

        if (opponentSpawnButtonLabel == null)
            opponentSpawnButtonLabel = FindChildText("SetOpponentSpawnLabel");
    }

    private Text FindChildText(string objectName)
    {
        Text[] labels = spawnPlacementPanel.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && labels[i].gameObject.name == objectName)
                return labels[i];
        }

        return null;
    }

    private void UpdateSpawnButtonLabels()
    {
        CacheSpawnButtonLabels();

        if (CanLocalControlSpawnPlacement())
        {
            SetLabelText(mySpawnButtonLabel, setMySpawnText);
            SetLabelText(opponentSpawnButtonLabel, setEnemySpawnText);
            return;
        }

        bool mySpawnSlotIsLocalPlayer = IsMySpawnSlotLocalPlayer();
        SetLabelText(mySpawnButtonLabel, mySpawnSlotIsLocalPlayer ? mySpawnDisplayText : enemySpawnDisplayText);
        SetLabelText(opponentSpawnButtonLabel, mySpawnSlotIsLocalPlayer ? enemySpawnDisplayText : mySpawnDisplayText);
    }

    private void SetLabelText(Text label, string text)
    {
        if (label != null)
            label.text = text;
    }

    private void SetupSpawnButtonBackgrounds()
    {
        CacheSpawnButtonLabels();
        mySpawnButtonBackground = EnsureLabeledButtonBackground(mySpawnButtonLabel, "SetMySpawnButtonBackground", SetMySpawnMode);
        opponentSpawnButtonBackground = EnsureLabeledButtonBackground(opponentSpawnButtonLabel, "SetOpponentSpawnButtonBackground", SetOpponentSpawnMode);
        UpdateSpawnButtonVisuals();
    }

    private Image EnsureLabeledButtonBackground(Text label, string backgroundName, UnityAction clickAction)
    {
        if (label == null || label.transform.parent == null)
            return null;

        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;

        Transform parent = label.transform.parent;
        Transform existingBackground = parent.Find(backgroundName);
        GameObject backgroundObject = existingBackground != null ? existingBackground.gameObject : new GameObject(backgroundName);
        backgroundObject.transform.SetParent(parent, false);

        RectTransform labelRect = label.rectTransform;
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        if (backgroundRect == null)
            backgroundRect = backgroundObject.AddComponent<RectTransform>();

        backgroundRect.anchorMin = labelRect.anchorMin;
        backgroundRect.anchorMax = labelRect.anchorMax;
        backgroundRect.anchoredPosition = new Vector2(0f, labelRect.anchoredPosition.y);
        backgroundRect.sizeDelta = new Vector2(GetStretchedButtonWidthDelta(labelRect, spawnButtonHorizontalPadding), Mathf.Max(labelRect.sizeDelta.y, spawnButtonMinHeight));
        backgroundRect.pivot = labelRect.pivot;
        backgroundRect.localScale = Vector3.one;
        CopyRectTransform(labelRect, backgroundRect);

        Image image = backgroundObject.GetComponent<Image>();
        if (image == null)
            image = backgroundObject.AddComponent<Image>();

        image.color = spawnButtonBackgroundColor;
        image.raycastTarget = true;

        Button button = backgroundObject.GetComponent<Button>();
        if (button == null)
            button = backgroundObject.AddComponent<Button>();

        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(clickAction);

        backgroundObject.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
        label.transform.SetAsLastSibling();
        return image;
    }

    private void CopyRectTransform(RectTransform target, RectTransform source)
    {
        if (target == null || source == null)
            return;

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localScale = source.localScale;
    }

    private float GetStretchedButtonWidthDelta(RectTransform rectTransform, float horizontalPadding)
    {
        if (rectTransform != null && !Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x))
            return -Mathf.Abs(horizontalPadding * 2f);

        return Mathf.Max(rectTransform != null ? rectTransform.sizeDelta.x : 0f, 72f);
    }

    private void UpdateSpawnButtonVisuals()
    {
        if (!CanLocalControlSpawnPlacement())
        {
            bool mySpawnSlotIsLocalPlayer = IsMySpawnSlotLocalPlayer();

            if (mySpawnButtonBackground != null)
                mySpawnButtonBackground.color = mySpawnSlotIsLocalPlayer ? mySpawnButtonSelectedColor : enemySpawnButtonSelectedColor;

            if (opponentSpawnButtonBackground != null)
                opponentSpawnButtonBackground.color = mySpawnSlotIsLocalPlayer ? enemySpawnButtonSelectedColor : mySpawnButtonSelectedColor;

            return;
        }

        if (mySpawnButtonBackground != null)
            mySpawnButtonBackground.color = currentMode == SpawnMode.MySpawn ? mySpawnButtonSelectedColor : spawnButtonBackgroundColor;

        if (opponentSpawnButtonBackground != null)
            opponentSpawnButtonBackground.color = currentMode == SpawnMode.OpponentSpawn ? enemySpawnButtonSelectedColor : spawnButtonBackgroundColor;
    }

    private void RestoreSavedMarkers()
    {
        if (dataStore == null || dataStore.spawnData == null)
            return;

        if (dataStore.spawnData.hasMySpawn)
            UpdateMarker(mySpawnMarker, dataStore.spawnData.mySpawnPosition);

        if (dataStore.spawnData.hasOpponentSpawn)
            UpdateMarker(opponentSpawnMarker, dataStore.spawnData.opponentSpawnPosition);
    }

    private bool EnsurePrefabSpawnMarkers()
    {
        GameObject mySlotPrefab = GetSpawnMarkerPrefabForDataSlot(true);
        GameObject opponentSlotPrefab = GetSpawnMarkerPrefabForDataSlot(false);

        bool changed = false;
        mySpawnMarker = EnsurePrefabSpawnMarker(mySlotPrefab, mySpawnMarker, "MySpawnMarker", ref activeMySpawnMarkerPrefab, ref changed);
        opponentSpawnMarker = EnsurePrefabSpawnMarker(opponentSlotPrefab, opponentSpawnMarker, "OpponentSpawnMarker", ref activeOpponentSpawnMarkerPrefab, ref changed);
        return changed;
    }

    private GameObject GetSpawnMarkerPrefabForDataSlot(bool isMySpawnSlot)
    {
        bool mySpawnSlotIsLocalPlayer = IsMySpawnSlotLocalPlayer();
        bool slotBelongsToLocalPlayer = isMySpawnSlot ? mySpawnSlotIsLocalPlayer : !mySpawnSlotIsLocalPlayer;
        GameObject preferredPrefab = slotBelongsToLocalPlayer ? mySpawnMarkerPrefab : opponentSpawnMarkerPrefab;
        GameObject fallbackPrefab = isMySpawnSlot ? mySpawnMarkerPrefab : opponentSpawnMarkerPrefab;
        return preferredPrefab != null ? preferredPrefab : fallbackPrefab;
    }

    private bool IsMySpawnSlotLocalPlayer()
    {
        if (LobbyState.Instance == null || LobbyState.Instance.Runner == null)
            return true;

        bool localIsHost = LobbyState.Instance.Runner.IsServer;
        bool spawnOwnerIsHost = !LobbyState.Instance.objectPlacementAuthorityIsHost;
        return localIsHost == spawnOwnerIsHost;
    }

    private GameObject EnsurePrefabSpawnMarker(GameObject markerPrefab, GameObject currentMarker, string markerName, ref GameObject activeMarkerPrefab, ref bool changed)
    {
        if (markerPrefab == null)
            return currentMarker;

        if (currentMarker != null && activeMarkerPrefab == markerPrefab)
            return currentMarker;

        Transform parent = currentMarker != null && currentMarker.transform.parent != null
            ? currentMarker.transform.parent
            : transform;

        Vector3 position = currentMarker != null ? currentMarker.transform.position : Vector3.zero;
        bool active = currentMarker != null && currentMarker.activeSelf;

        if (currentMarker != null)
        {
            if (activeMarkerPrefab != null)
                Destroy(currentMarker);
            else
                currentMarker.SetActive(false);
        }

        GameObject marker = Instantiate(markerPrefab, parent);
        marker.name = markerName;
        marker.transform.position = position;
        marker.transform.rotation = markerPrefab.transform.rotation;
        marker.transform.localScale = markerPrefab.transform.localScale;
        FitMarkerToGridCell(marker);
        marker.SetActive(active);
        SetCollidersEnabled(marker, false);

        activeMarkerPrefab = markerPrefab;
        changed = true;
        return marker;
    }

    private void FitMarkerToGridCell(GameObject marker)
    {
        if (!fitSpawnMarkersToGridCell || marker == null)
            return;

        Bounds bounds;
        if (!TryGetMarkerBounds(marker, out bounds))
            return;

        float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
        if (horizontalSize <= 0.001f)
            return;

        float safeGridSize = Mathf.Max(0.01f, Mathf.Abs(gridSize));
        float targetSize = safeGridSize * Mathf.Clamp(spawnMarkerCellFill, 0.1f, 1f);
        marker.transform.localScale *= targetSize / horizontalSize;
    }

    private bool TryGetMarkerBounds(GameObject marker, out Bounds bounds)
    {
        Renderer[] renderers = marker.GetComponentsInChildren<Renderer>(true);
        if (TryCollectMarkerBounds(renderers, false, out bounds))
            return true;

        return TryCollectMarkerBounds(renderers, true, out bounds);
    }

    private bool TryCollectMarkerBounds(Renderer[] renderers, bool includeParticles, out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!includeParticles && renderer is ParticleSystemRenderer)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void DisableMarkerColliders()
    {
        SetCollidersEnabled(mySpawnMarker, false);
        SetCollidersEnabled(opponentSpawnMarker, false);
    }

    private void SetCollidersEnabled(GameObject target, bool enabled)
    {
        if (target == null)
            return;

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = enabled;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer == -1)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void SetSpawnPlacementShadowsHidden(bool hidden)
    {
        if (hidden)
            HideSpawnPlacementShadows();
        else
            RestoreSpawnPlacementShadows();
    }

    private void HideSpawnPlacementShadows()
    {
        Light[] lights = GetSpawnPlacementShadowLights();
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null)
                continue;

            if (!originalSpawnLightShadows.ContainsKey(light))
                originalSpawnLightShadows.Add(light, light.shadows);

            light.shadows = LightShadows.None;
        }

        spawnPlacementShadowsHidden = true;
    }

    private void RestoreSpawnPlacementShadows()
    {
        if (!spawnPlacementShadowsHidden && originalSpawnLightShadows.Count == 0)
            return;

        foreach (KeyValuePair<Light, LightShadows> entry in originalSpawnLightShadows)
        {
            if (entry.Key != null)
                entry.Key.shadows = entry.Value;
        }

        originalSpawnLightShadows.Clear();
        spawnPlacementShadowsHidden = false;
    }

    private Light[] GetSpawnPlacementShadowLights()
    {
        if (spawnPlacementShadowLights != null && spawnPlacementShadowLights.Length > 0)
            return spawnPlacementShadowLights;

        return FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }
    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }
}
