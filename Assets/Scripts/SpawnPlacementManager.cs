using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Layers")]
    public LayerMask boardLayer;
    public LayerMask placedObjectMask;

    [Header("Settings")]
    public float gridSize = 1f;
    public float markerYOffset = 0.3f;
    public Vector3 spawnCheckHalfExtents = new Vector3(0.4f, 1f, 0.4f);

    private SpawnMode currentMode = SpawnMode.None;

    private void OnEnable()
    {
        LobbyState.PrepSpawnPlaced += HandleNetworkSpawnPlaced;
    }

    private void OnDisable()
    {
        LobbyState.PrepSpawnPlaced -= HandleNetworkSpawnPlaced;
    }

    private void Start()
    {
        EnsureSpawnButtonsCanReceiveClicks();
        DisableMarkerColliders();
        RestoreSavedMarkers();
    }

    private void Update()
    {
        if (spawnPlacementPanel == null || !spawnPlacementPanel.activeInHierarchy)
            return;

        if (!CanLocalControlSpawnPlacement())
            return;

        if (currentMode == SpawnMode.None)
            return;

        if (IsPointerBlockedByUi())
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Vector3 snappedPosition;
        if (!TryGetSnappedBoardPoint(out snappedPosition))
            return;

        if (!CanPlaceSpawn(snappedPosition))
            return;

        if (currentMode == SpawnMode.MySpawn)
        {
            RequestSpawnPlacement(true, snappedPosition);
        }
        else if (currentMode == SpawnMode.OpponentSpawn)
        {
            RequestSpawnPlacement(false, snappedPosition);
        }
    }

    public void SetMySpawnMode()
    {
        if (!CanLocalControlSpawnPlacement())
            return;

        currentMode = SpawnMode.MySpawn;
    }

    public void SetOpponentSpawnMode()
    {
        if (!CanLocalControlSpawnPlacement())
            return;

        currentMode = SpawnMode.OpponentSpawn;
    }

    public void ClearMode()
    {
        currentMode = SpawnMode.None;
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
        snappedPosition.x = Mathf.Round(snappedPosition.x / safeGridSize) * safeGridSize;
        snappedPosition.z = Mathf.Round(snappedPosition.z / safeGridSize) * safeGridSize;
        snappedPosition.y = GetBoardTopY() + markerYOffset;

        return true;
    }

    private bool CanPlaceSpawn(Vector3 position)
    {
        if (boardCollider == null)
            return false;

        Bounds bounds = boardCollider.bounds;
        bool insideBoard =
            position.x >= bounds.min.x &&
            position.x <= bounds.max.x &&
            position.z >= bounds.min.z &&
            position.z <= bounds.max.z;

        if (!insideBoard)
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
        return LobbyState.Instance != null && LobbyState.Instance.LocalHasSpawnPlacementAuthority();
    }

    private float GetBoardTopY()
    {
        return boardCollider != null ? boardCollider.bounds.max.y : 0f;
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
}
