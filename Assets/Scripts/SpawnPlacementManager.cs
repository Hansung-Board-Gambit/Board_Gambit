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

    [Header("Button Labels")]
    public Text mySpawnButtonLabel;
    public Text opponentSpawnButtonLabel;
    public string setMySpawnText = "Set My Spawn";
    public string setEnemySpawnText = "Set Enemy Spawn";
    public float spawnButtonMinHeight = 56f;
    public float spawnButtonHorizontalPadding = 12f;
    public Color spawnButtonBackgroundColor = new Color(0.08f, 0.09f, 0.13f, 0.95f);
    public Color spawnButtonSelectedColor = new Color(0.12f, 0.32f, 0.58f, 0.95f);

    [Header("Layers")]
    public LayerMask boardLayer;
    public LayerMask placedObjectMask;

    [Header("Settings")]
    public float gridSize = 1f;
    public int blockedEdgeCellCount = 0;
    public bool useCircularBoardBounds = false;
    public float markerYOffset = 0.3f;
    public Vector3 spawnCheckHalfExtents = new Vector3(0.4f, 1f, 0.4f);

    [Header("Editor Test")]
    public bool allowEditorLocalTest = false;

    private SpawnMode currentMode = SpawnMode.None;
    private GameObject previewMarker;
    private SpawnMode previewMarkerMode = SpawnMode.None;
    private Image mySpawnButtonBackground;
    private Image opponentSpawnButtonBackground;

    private void OnEnable()
    {
        LobbyState.PrepSpawnPlaced += HandleNetworkSpawnPlaced;
    }

    private void OnDisable()
    {
        LobbyState.PrepSpawnPlaced -= HandleNetworkSpawnPlaced;
        DestroyPreviewMarker();
    }

    private void Start()
    {
        EnsureSpawnButtonsCanReceiveClicks();
        CacheSpawnButtonLabels();
        UpdateSpawnButtonLabels();
        SetupSpawnButtonBackgrounds();
        UpdateSpawnButtonVisuals();
        DisableMarkerColliders();
        RestoreSavedMarkers();
    }

    private void Update()
    {
        if (spawnPlacementPanel == null || !spawnPlacementPanel.activeInHierarchy)
        {
            SetPreviewMarkerActive(false);
            return;
        }

        UpdateSpawnButtonLabels();
        UpdateSpawnButtonVisuals();

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
            SetPreviewMarkerActive(false);
            return;
        }

        UpdatePreviewMarker(snappedPosition);

        if (!Input.GetMouseButtonDown(0))
            return;

        if (currentMode == SpawnMode.MySpawn)
        {
            RequestSpawnPlacement(true, snappedPosition);
        }
        else if (currentMode == SpawnMode.OpponentSpawn)
        {
            RequestSpawnPlacement(false, snappedPosition);
        }

        SetPreviewMarkerActive(false);
    }

    public void SetMySpawnMode()
    {
        if (!CanLocalControlSpawnPlacement())
            return;

        currentMode = SpawnMode.MySpawn;
        UpdateSpawnButtonVisuals();
    }

    public void SetOpponentSpawnMode()
    {
        if (!CanLocalControlSpawnPlacement())
            return;

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

        bool localOwnsSpawnPlacement = CanLocalControlSpawnPlacement();
        SetLabelText(mySpawnButtonLabel, localOwnsSpawnPlacement ? setMySpawnText : setEnemySpawnText);
        SetLabelText(opponentSpawnButtonLabel, localOwnsSpawnPlacement ? setEnemySpawnText : setMySpawnText);
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
        if (mySpawnButtonBackground != null)
            mySpawnButtonBackground.color = currentMode == SpawnMode.MySpawn ? spawnButtonSelectedColor : spawnButtonBackgroundColor;

        if (opponentSpawnButtonBackground != null)
            opponentSpawnButtonBackground.color = currentMode == SpawnMode.OpponentSpawn ? spawnButtonSelectedColor : spawnButtonBackgroundColor;
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
}
