using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlacementManager : MonoBehaviour
{
    private enum PlacementToolMode
    {
        Place,
        Delete
    }

    private class SlotPreviewBinding
    {
        public RawImage image;
        public RenderTexture texture;
        public GameObject worldObject;
    }

    [Header("References")]
    public Camera mainCamera;
    public GameObject objectPlacementPanel;
    public Collider boardCollider;
    public Transform placedParent;
    public PrepDataStore dataStore;
    public Text pointText;

    [Header("Layers")]
    public LayerMask boardLayer;
    public LayerMask placedObjectMask;
    public string placedObjectLayerName = "PlacedObject";

    [Header("Placement Settings")]
    public float gridSize = 1f;
    public int blockedEdgeCellCount = 0;
    public bool useCircularBoardBounds = false;
    public GameObject[] placeablePrefabs;

    [Header("Rotation")]
    public KeyCode rotateKey = KeyCode.R;

    [Header("Auto Fit")]
    public bool autoScaleObjectsToFootprint = true;
    public float footprintFitPadding = 0.85f;
    public bool alignObjectsToBoardSurface = true;
    public float boardSurfaceYOffset = 0.02f;

    [Header("Slot UI")]
    public bool randomizeSlotsOnStart = true;
    public bool repeatPrefabsForExtraSlots = true;
    public string emptySlotLabel = "Empty";

    [Header("Slot Preview")]
    public bool showSlotPrefabPreview = true;
    public int slotPreviewTextureSize = 128;
    public float slotPreviewPadding = 10f;
    public Color slotPreviewBackgroundColor = new Color(0f, 0f, 0f, 0f);
    public Vector3 slotPreviewWorldOrigin = new Vector3(5000f, 5000f, 5000f);

    [Header("Tool UI")]
    public string placeButtonText = "Place";
    public string deleteButtonText = "Delete";
    public string rotateHintText = "Press R to Rotate";
    public float toolButtonMinHeight = 44f;
    public float toolButtonHorizontalPadding = 12f;
    public Color toolButtonNormalColor = new Color(0.08f, 0.09f, 0.13f, 0.95f);
    public Color toolButtonSelectedColor = new Color(0.12f, 0.32f, 0.58f, 0.95f);

    [Header("Delete")]
    public float deletePositionTolerance = 0.2f;

    [Header("Editor Test")]
    public bool allowEditorLocalTest = false;

    [Header("Sound")]
    public AudioSource sfxSource;
    public AudioClip selectSfx;
    public AudioClip placeSfx;
    public AudioClip deleteSfx;
    public AudioClip rotateSfx;
    public AudioClip errorSfx;

    private const float PlacementRotationStepDegrees = 90f;

    private bool wasCantPlace;
    private PlacementToolMode currentToolMode = PlacementToolMode.Place;
    private int selectedIndex = -1;
    private int placementRotationSteps;
    private GameObject previewObject;
    private int[] slotPrefabIndices;
    private GameObject slotPreviewRoot;
    private Image placeModeButtonBackground;
    private Image deleteModeButtonBackground;
    private readonly List<SlotPreviewBinding> slotPreviewBindings = new List<SlotPreviewBinding>();
    private HashSet<Vector3> deleteSfxCooldown = new HashSet<Vector3>();

    private IEnumerator RemoveDeleteCooldown(Vector3 pos)
    {
        yield return null; // 1프레임만 막기
        deleteSfxCooldown.Remove(pos);
    }

    private void OnEnable()
    {
        LobbyState.PrepObjectPlaced += HandleNetworkObjectPlaced;
        LobbyState.PrepObjectDeleted += HandleNetworkObjectDeleted;
    }

    private void OnDisable()
    {
        LobbyState.PrepObjectPlaced -= HandleNetworkObjectPlaced;
        LobbyState.PrepObjectDeleted -= HandleNetworkObjectDeleted;
    }

    private void OnDestroy()
    {
        DestroySlotPreviews();
    }

    private void Start()
    {
        SetupPlacementToolUi();
        RefreshObjectSlots();
        SetPlaceMode();
        UpdatePointText();
    }

    private void Update()
    {
        if (objectPlacementPanel == null || !objectPlacementPanel.activeInHierarchy)
        {
            SetPreviewActive(false);
            return;
        }

        UpdatePointText();

        if (!CanLocalControlPlacement())
        {
            SetPreviewActive(false);
            return;
        }

        if (currentToolMode == PlacementToolMode.Delete)
        {
            SetPreviewActive(false);

            if (IsPointerBlockedByUi())
                return;

            if (Input.GetMouseButtonDown(0))
                TryDeletePlacedObject();

            return;
        }

        if (!HasValidSelection())
        {
            SetPreviewActive(false);
            return;
        }

        if (Input.GetKeyDown(rotateKey))
            RotateSelection();

        if (IsPointerBlockedByUi())
            return;

        Quaternion placementRotation = GetPlacementRotation();
        PlaceableObject selectedInfo = placeablePrefabs[selectedIndex].GetComponent<PlaceableObject>();
        Vector3 snappedPosition;
        if (!TryGetSnappedBoardPoint(selectedInfo, placementRotation, out snappedPosition))
        {
            SetPreviewActive(false);
            return;
        }

        EnsurePreviewExists();
        if (previewObject == null)
            return;

        PlaceableObject previewInfo = previewObject.GetComponent<PlaceableObject>();
        snappedPosition.y = GetPlacementY(previewInfo);
        previewObject.transform.rotation = placementRotation;
        previewObject.transform.position = snappedPosition;
        AlignObjectToBoardSurface(previewObject);
        previewObject.SetActive(true);

        bool canPlace = CanPlace(snappedPosition, previewInfo, placementRotation);
        SetPreviewColor(canPlace ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f));

        wasCantPlace = false;

        if (Input.GetMouseButtonDown(0))
        {
            PlaceSelectedObject(snappedPosition, canPlace);
        }
    }

    public void SelectObject(int index)
    {
        if (!CanLocalControlPlacement())
            return;

        if (slotPrefabIndices == null || slotPrefabIndices.Length == 0)
            SetupObjectSlots();

        int prefabIndex;
        if (!TryGetPrefabIndexForSlotIndex(index, out prefabIndex))
        {
            CancelSelection();
            Debug.LogWarning("SelectObject ignored because no prefab is assigned for slot index = " + index);
            return;
        }
        PlaySfx(selectSfx);

        Debug.Log("SelectObject called, slot index = " + index + ", prefab index = " + prefabIndex);

        SetPlacementToolMode(PlacementToolMode.Place);
        selectedIndex = prefabIndex;
        placementRotationSteps = 0;
        DestroyPreview();
        //EnsurePreviewExists();
    }

    public void SetPlaceMode()
    {
        PlaySfx(selectSfx);
        SetPlacementToolMode(PlacementToolMode.Place);
    }

    public void SetDeleteMode()
    {
        PlaySfx(selectSfx);
        SetPlacementToolMode(PlacementToolMode.Delete);
    }

    public void CancelSelection()
    {
        selectedIndex = -1;
        placementRotationSteps = 0;
        DestroyPreview();
    }

    public void RefreshObjectSlots()
    {
        SetupObjectSlots();
    }

    private bool TryGetSnappedBoardPoint(PlaceableObject placementInfo, Quaternion rotation, out Vector3 snappedPosition)
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
        Vector2 footprint = GetFootprint(placementInfo, rotation);
        snappedPosition = hit.point;

        if (boardCollider != null)
        {
            Bounds bounds = boardCollider.bounds;
            snappedPosition.x = SnapToBoardFootprint(snappedPosition.x, bounds.min.x, bounds.max.x, safeGridSize, footprint.x);
            snappedPosition.z = SnapToBoardFootprint(snappedPosition.z, bounds.min.z, bounds.max.z, safeGridSize, footprint.y);
        }
        else
        {
            snappedPosition.x = Mathf.Round(snappedPosition.x / safeGridSize) * safeGridSize;
            snappedPosition.z = Mathf.Round(snappedPosition.z / safeGridSize) * safeGridSize;
        }

        return true;
    }

    private void EnsurePreviewExists()
    {
        if (previewObject != null)
            return;

        if (!HasValidSelection())
            return;

        GameObject prefab = placeablePrefabs[selectedIndex];
        previewObject = Instantiate(prefab);
        previewObject.name = prefab.name + "_Preview";
        previewObject.transform.position = Vector3.zero;
        previewObject.transform.rotation = GetPlacementRotation();
        previewObject.transform.localScale = prefab.transform.localScale;

        PlaceableObject previewInfo = previewObject.GetComponent<PlaceableObject>();
        EnableRenderers(previewObject);
        ApplyFootprintScale(previewObject, previewInfo, GetPlacementRotation());

        DisableColliders(previewObject);
        SetLayerRecursively(previewObject, LayerMask.NameToLayer("Ignore Raycast"));
    }

    private bool CanPlace(Vector3 position, PlaceableObject info, Quaternion rotation)
    {
        if (dataStore == null || !dataStore.CanSpendPoint())
            return false;

        if (boardCollider == null)
            return false;

        Vector2 footprint = GetFootprint(info, rotation);

        Bounds bounds = boardCollider.bounds;
        float safeGridSize = Mathf.Max(0.01f, Mathf.Abs(gridSize));

        float halfX = footprint.x * safeGridSize * 0.5f;
        float halfZ = footprint.y * safeGridSize * 0.5f;

        float edgeMargin = GetBlockedEdgeMargin();
        bool insideBoard =
            position.x - halfX >= bounds.min.x + edgeMargin &&
            position.x + halfX <= bounds.max.x - edgeMargin &&
            position.z - halfZ >= bounds.min.z + edgeMargin &&
            position.z + halfZ <= bounds.max.z - edgeMargin;

        if (!insideBoard)
            return false;

        if (useCircularBoardBounds && !IsFootprintInsideCircularBoard(position, halfX, halfZ, bounds, edgeMargin))
            return false;

        Vector3 center = new Vector3(position.x, bounds.center.y + 0.5f, position.z);
        Vector3 halfExtents = new Vector3(
            Mathf.Max(0.05f, halfX - 0.05f),
            1f,
            Mathf.Max(0.05f, halfZ - 0.05f)
        );

        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, Quaternion.identity, placedObjectMask);
        return overlaps.Length == 0;
    }

    private void PlaceSelectedObject(Vector3 position, bool canPlace)
    {
        Quaternion rotation = GetPlacementRotation();
        PlaceableObject info = placeablePrefabs[selectedIndex].GetComponent<PlaceableObject>();

        if (!dataStore.CanSpendPoint())
        {
            PlaySfx(errorSfx); // 포인트 부족은 항상 울림
            return;
        }

        if (!canPlace)
        {
            PlaySfx(errorSfx); // 빨간 상태 클릭 시 1회 울림
            return;
        }

        PlaySfx(placeSfx); // 성공 시에만

        int prefabIndex = selectedIndex;
        CancelSelection();

        if (LobbyState.Instance != null)
        {
            LobbyState.Instance.RequestPlacePrepObject(prefabIndex, position, rotation);
            return;
        }

        CreatePlacedObject(prefabIndex, position, rotation);
    }

    private void HandleNetworkObjectPlaced(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        CreatePlacedObject(prefabIndex, position, rotation);
    }

    private void HandleNetworkObjectDeleted(Vector3 position)
    {
        DeletePlacedObjectAt(position);
    }

    private void CreatePlacedObject(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        if (dataStore == null || !IsValidPrefabIndex(prefabIndex))
            return;

        GameObject prefab = placeablePrefabs[prefabIndex];
        GameObject placed = Instantiate(prefab);
        if (placed == null)
            return;

        int placedCount = dataStore.placedObjects != null ? dataStore.placedObjects.Count + 1 : 1;
        placed.name = prefab.name + "_Placed_" + placedCount;

        if (placedParent != null)
            placed.transform.SetParent(placedParent, true);

        PlaceableObject info = placed.GetComponent<PlaceableObject>();

        placed.transform.position = position;
        placed.transform.rotation = rotation;
        placed.transform.localScale = prefab.transform.localScale;
        placed.SetActive(true);

        int enabledRenderers = EnableRenderers(placed);
        ApplyFootprintScale(placed, info, rotation);
        AlignObjectToBoardSurface(placed);
        int createdColliders = EnsurePlacementCollider(placed);
        int enabledColliders = EnableColliders(placed);
        int frozenRigidbodies = FreezePlacedRigidbodies(placed);
        int fixedMaterials = EnsureMaterialsVisible(placed);

        int placedLayer = LayerMask.NameToLayer(placedObjectLayerName);
        if (placedLayer != -1)
        {
            SetLayerRecursively(placed, placedLayer);
            EnsureMainCameraRendersLayer(placedLayer);
        }
        else
        {
            Debug.LogWarning("PlacedObject layer was not found. Keeping placed object on layer " + LayerMask.LayerToName(placed.layer));
        }

        string id = info != null ? info.prefabId : prefab.name;

        dataStore.SavePlacedObject(id, placed.transform.position, placed.transform.rotation);
        dataStore.SpendPoint();
        UpdatePointText();

        Debug.Log(
            "Placed object created: name=" + placed.name +
            ", position=" + placed.transform.position +
            ", rotation=" + placed.transform.rotation.eulerAngles +
            ", scale=" + placed.transform.lossyScale +
            ", parent=" + (placed.transform.parent != null ? placed.transform.parent.name : "None") +
            ", layer=" + LayerMask.LayerToName(placed.layer) +
            ", active=" + placed.activeInHierarchy +
            ", renderersEnabled=" + enabledRenderers +
            ", collidersEnabled=" + enabledColliders +
            ", collidersCreated=" + createdColliders +
            ", rigidbodiesFrozen=" + frozenRigidbodies +
            ", materialsFixed=" + fixedMaterials
        );

        SetPreviewActive(false);
    }

    private void TryDeletePlacedObject()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 500f, placedObjectMask, QueryTriggerInteraction.Ignore))
            return;

        GameObject placedObject = FindPlacedObjectRoot(hit.collider);
        if (placedObject == null)
            return;

        Vector3 deletePosition = placedObject.transform.position;
        if (LobbyState.Instance != null)
        {
            LobbyState.Instance.RequestDeletePrepObject(deletePosition);
            return;
        }

        DeletePlacedObjectAt(deletePosition);
    }

    private GameObject FindPlacedObjectRoot(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        Transform current = hitCollider.transform;
        if (placedParent != null)
        {
            while (current != null && current.parent != placedParent)
                current = current.parent;

            if (current != null)
                return current.gameObject;
        }

        PlaceableObject placeableObject = hitCollider.GetComponentInParent<PlaceableObject>();
        if (placeableObject != null)
            return placeableObject.gameObject;

        return hitCollider.gameObject;
    }

    private void DeletePlacedObjectAt(Vector3 position)
    {
        GameObject placedObject = FindPlacedObjectByPosition(position);

        if (placedObject != null)
        {
            Destroy(placedObject);

            // 중복 방지 (한 프레임/한 위치 기준)
            if (!deleteSfxCooldown.Contains(position))
            {
                PlaySfx(deleteSfx);
                deleteSfxCooldown.Add(position);
                StartCoroutine(RemoveDeleteCooldown(position));
            }
        }

        if (dataStore != null && dataStore.RemovePlacedObject(position, deletePositionTolerance))
        {
            dataStore.RefundPoint();
            UpdatePointText();
        }
    }

    private GameObject FindPlacedObjectByPosition(Vector3 position)
    {
        if (placedParent == null)
            return null;

        float toleranceSqr = Mathf.Max(0.001f, deletePositionTolerance) * Mathf.Max(0.001f, deletePositionTolerance);
        GameObject closestObject = null;
        float closestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < placedParent.childCount; i++)
        {
            Transform child = placedParent.GetChild(i);
            float distanceSqr = (child.position - position).sqrMagnitude;
            if (distanceSqr > toleranceSqr || distanceSqr >= closestDistanceSqr)
                continue;

            closestDistanceSqr = distanceSqr;
            closestObject = child.gameObject;
        }

        return closestObject;
    }

    private void UpdatePointText()
    {
        if (pointText != null && dataStore != null)
            pointText.text = "Available Object Point : " + dataStore.remainingPoints;
    }

    private void SetPreviewActive(bool active)
    {
        if (previewObject != null)
            previewObject.SetActive(active);
    }

    private void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }

    private void DisableColliders(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private int EnableColliders(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = true;

        return colliders.Length;
    }

    private int EnableRenderers(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = true;

        return renderers.Length;
    }

    private int EnsurePlacementCollider(GameObject target)
    {
        if (target.GetComponentsInChildren<Collider>(true).Length > 0)
            return 0;

        Bounds bounds;
        if (!TryGetRendererBounds(target, out bounds))
            return 0;

        BoxCollider boxCollider = target.AddComponent<BoxCollider>();
        Vector3 localSize = target.transform.InverseTransformVector(bounds.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        boxCollider.center = target.transform.InverseTransformPoint(bounds.center);
        boxCollider.size = new Vector3(
            Mathf.Max(0.05f, localSize.x),
            Mathf.Max(0.05f, localSize.y),
            Mathf.Max(0.05f, localSize.z)
        );

        return 1;
    }

    private int FreezePlacedRigidbodies(GameObject target)
    {
        Rigidbody[] rigidbodies = target.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        return rigidbodies.Length;
    }

    private int EnsureMaterialsVisible(GameObject target)
    {
        int fixedMaterials = 0;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material mat = materials[j];
                if (mat == null)
                    continue;

                fixedMaterials += EnsureMaterialColorVisible(mat, "_Color");
                fixedMaterials += EnsureMaterialColorVisible(mat, "_BaseColor");
            }
        }

        return fixedMaterials;
    }

    private int EnsureMaterialColorVisible(Material mat, string propertyName)
    {
        if (!mat.HasProperty(propertyName))
            return 0;

        Color color = mat.GetColor(propertyName);
        if (color.a > 0.01f)
            return 0;

        color.a = 1f;
        mat.SetColor(propertyName, color);
        return 1;
    }

    private void SetPreviewColor(Color color)
    {
        if (previewObject == null)
            return;

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material mat = renderers[i].material;
            if (mat.HasProperty("_Color"))
                mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
        }
    }

    private bool HasValidSelection()
    {
        return IsValidPrefabIndex(selectedIndex);
    }

    private void SetPlacementToolMode(PlacementToolMode mode)
    {
        currentToolMode = mode;

        if (currentToolMode == PlacementToolMode.Delete)
            CancelSelection();

        UpdatePlacementToolButtonVisuals();
    }

    private bool CanLocalControlPlacement()
    {
        if (LobbyState.Instance != null)
            return LobbyState.Instance.LocalHasObjectPlacementAuthority();

#if UNITY_EDITOR
        return allowEditorLocalTest;
#else
        return false;
#endif
    }

    private bool IsValidPrefabIndex(int index)
    {
        return placeablePrefabs != null &&
               index >= 0 &&
               index < placeablePrefabs.Length &&
               placeablePrefabs[index] != null;
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

    private float GetBoardTopY()
    {
        return boardCollider != null ? boardCollider.bounds.max.y : 0f;
    }

    private float GetPlacementY(PlaceableObject info)
    {
        if (alignObjectsToBoardSurface)
            return GetBoardTopY();

        return GetBoardTopY() + (info != null ? info.yOffset : 0f);
    }

    private void ApplyFootprintScale(GameObject target, PlaceableObject info, Quaternion rotation)
    {
        if (!autoScaleObjectsToFootprint || target == null || info == null)
            return;

        Bounds bounds;
        if (!TryGetRendererBounds(target, out bounds))
            return;

        Vector2 footprint = GetFootprint(info, rotation);
        float safeGridSize = Mathf.Max(0.01f, Mathf.Abs(gridSize));
        float targetWidth = footprint.x * safeGridSize * Mathf.Clamp(footprintFitPadding, 0.1f, 1f);
        float targetDepth = footprint.y * safeGridSize * Mathf.Clamp(footprintFitPadding, 0.1f, 1f);

        float scaleX = bounds.size.x > 0.001f ? targetWidth / bounds.size.x : float.PositiveInfinity;
        float scaleZ = bounds.size.z > 0.001f ? targetDepth / bounds.size.z : float.PositiveInfinity;
        float scaleFactor = Mathf.Min(scaleX, scaleZ);

        if (float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor) || scaleFactor <= 0f)
            return;

        target.transform.localScale *= scaleFactor;
    }

    private void AlignObjectToBoardSurface(GameObject target)
    {
        if (!alignObjectsToBoardSurface || target == null)
            return;

        Bounds bounds;
        if (!TryGetRendererBounds(target, out bounds))
            return;

        Vector3 position = target.transform.position;
        position.y += GetBoardTopY() + boardSurfaceYOffset - bounds.min.y;
        target.transform.position = position;
    }

    private Vector2 GetFootprint(PlaceableObject info, Quaternion rotation)
    {
        Vector2 footprint = ResolveFootprint(info);
        if (IsQuarterTurnRotation(rotation))
            return new Vector2(footprint.y, footprint.x);

        return footprint;
    }

    private Vector2 ResolveFootprint(PlaceableObject info)
    {
        if (info == null)
            return Vector2.one;

        float footprintX = info.footprint.x;
        float footprintZ = info.footprint.y;

        if ((footprintX <= 0f || footprintZ <= 0f) && TryGetLocalRendererFootprintSize(info.gameObject, out Vector2 modelFootprintSize))
        {
            float modelX = Mathf.Max(0.001f, modelFootprintSize.x);
            float modelZ = Mathf.Max(0.001f, modelFootprintSize.y);

            if (footprintX > 0f && footprintZ <= 0f)
                footprintZ = footprintX * modelZ / modelX;
            else if (footprintZ > 0f && footprintX <= 0f)
                footprintX = footprintZ * modelX / modelZ;
        }

        if (footprintX <= 0f && footprintZ <= 0f)
        {
            footprintX = 1f;
            footprintZ = 1f;
        }
        else if (footprintX <= 0f)
        {
            footprintX = footprintZ;
        }
        else if (footprintZ <= 0f)
        {
            footprintZ = footprintX;
        }

        return new Vector2(Mathf.Max(1f, footprintX), Mathf.Max(1f, footprintZ));
    }

    private void RotateSelection()
    {
        PlaySfx(rotateSfx);
        placementRotationSteps = (placementRotationSteps + 1) % 4;

        if (previewObject == null)
            return;

        previewObject.transform.rotation = GetPlacementRotation();
        AlignObjectToBoardSurface(previewObject);
    }

    private Quaternion GetPlacementRotation()
    {
        return Quaternion.Euler(0f, placementRotationSteps * PlacementRotationStepDegrees, 0f);
    }

    private bool IsQuarterTurnRotation(Quaternion rotation)
    {
        int quarterTurns = Mathf.RoundToInt(Mathf.Repeat(rotation.eulerAngles.y, 360f) / PlacementRotationStepDegrees);
        return quarterTurns % 2 != 0;
    }

    private float SnapToBoardFootprint(float value, float min, float max, float safeGridSize, float footprintCells)
    {
        float firstLine = Mathf.Ceil(min / safeGridSize) * safeGridSize;
        float lastLine = Mathf.Floor(max / safeGridSize) * safeGridSize;
        int cellCount = Mathf.Max(1, Mathf.RoundToInt((lastLine - firstLine) / safeGridSize));
        int footprintCellCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(1f, footprintCells)), 1, cellCount);
        float halfFootprintSize = footprintCellCount * safeGridSize * 0.5f;
        int maxStartCell = Mathf.Max(0, cellCount - footprintCellCount);
        int startCell = Mathf.RoundToInt((value - firstLine - halfFootprintSize) / safeGridSize);
        startCell = Mathf.Clamp(startCell, 0, maxStartCell);
        return firstLine + halfFootprintSize + startCell * safeGridSize;
    }

    private float GetBlockedEdgeMargin()
    {
        float safeGridSize = Mathf.Max(0.01f, Mathf.Abs(gridSize));
        return Mathf.Max(0, blockedEdgeCellCount) * safeGridSize;
    }

    private bool IsFootprintInsideCircularBoard(Vector3 position, float halfX, float halfZ, Bounds bounds, float edgeMargin)
    {
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) - edgeMargin;
        if (radius <= 0f)
            return false;

        Vector2 center = new Vector2(bounds.center.x, bounds.center.z);
        float sqrRadius = radius * radius;

        return IsPointInsideCircle(new Vector2(position.x - halfX, position.z - halfZ), center, sqrRadius) &&
               IsPointInsideCircle(new Vector2(position.x - halfX, position.z + halfZ), center, sqrRadius) &&
               IsPointInsideCircle(new Vector2(position.x + halfX, position.z - halfZ), center, sqrRadius) &&
               IsPointInsideCircle(new Vector2(position.x + halfX, position.z + halfZ), center, sqrRadius);
    }

    private bool IsPointInsideCircle(Vector2 point, Vector2 center, float sqrRadius)
    {
        return (point - center).sqrMagnitude <= sqrRadius + 0.001f;
    }

    private void EnsureMainCameraRendersLayer(int layer)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null || layer < 0)
            return;

        int layerBit = 1 << layer;
        if ((mainCamera.cullingMask & layerBit) != 0)
            return;

        mainCamera.cullingMask |= layerBit;
        Debug.LogWarning("Main Camera culling mask did not include " + LayerMask.LayerToName(layer) + ". Added it at runtime.");
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

    private void SetupPlacementToolUi()
    {
        if (objectPlacementPanel == null)
            return;

        Text placeLabel = FindPanelText("SelectLabel");
        Text moveLabel = FindPanelText("MoveLabel");
        Text rotateLabel = FindPanelText("RotateLabel");
        Text deleteLabel = FindPanelText("DeleteLabel");

        RectTransform deleteHintSlot = deleteLabel != null ? CaptureRectTransform(deleteLabel.rectTransform) : null;

        if (moveLabel != null && deleteLabel != null)
            CopyRectTransform(deleteLabel.rectTransform, moveLabel.rectTransform);

        if (rotateLabel != null && deleteHintSlot != null)
            CopyRectTransform(rotateLabel.rectTransform, deleteHintSlot);

        if (deleteHintSlot != null)
            DestroySlotPreviewObject(deleteHintSlot.gameObject);

        if (placeLabel != null)
        {
            placeLabel.text = placeButtonText;
            placeModeButtonBackground = EnsureLabeledButtonBackground(placeLabel, "PlaceButtonBackground", SetPlaceMode);
        }

        if (deleteLabel != null)
        {
            deleteLabel.text = deleteButtonText;
            deleteModeButtonBackground = EnsureLabeledButtonBackground(deleteLabel, "DeleteButtonBackground", SetDeleteMode);
        }

        if (moveLabel != null)
            moveLabel.gameObject.SetActive(false);

        if (rotateLabel != null)
        {
            rotateLabel.text = GetRotateHintDisplayText();
            rotateLabel.raycastTarget = false;
            rotateLabel.fontSize = Mathf.Min(rotateLabel.fontSize, 15);
            rotateLabel.alignment = TextAnchor.MiddleCenter;
            RectTransform rotateRect = rotateLabel.rectTransform;
            rotateRect.anchoredPosition = new Vector2(0f, rotateRect.anchoredPosition.y);
            rotateRect.sizeDelta = new Vector2(GetStretchedButtonWidthDelta(rotateRect, toolButtonHorizontalPadding), Mathf.Max(rotateRect.sizeDelta.y, toolButtonMinHeight));
        }

        UpdatePlacementToolButtonVisuals();
    }

    private Text FindPanelText(string objectName)
    {
        if (objectPlacementPanel == null)
            return null;

        Text[] labels = objectPlacementPanel.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && labels[i].gameObject.name == objectName)
                return labels[i];
        }

        return null;
    }

    private RectTransform CaptureRectTransform(RectTransform source)
    {
        if (source == null)
            return null;

        GameObject snapshotObject = new GameObject(source.name + "_LayoutSnapshot");
        snapshotObject.hideFlags = HideFlags.HideAndDontSave;
        RectTransform snapshot = snapshotObject.AddComponent<RectTransform>();
        CopyRectTransform(snapshot, source);
        return snapshot;
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
        backgroundRect.sizeDelta = new Vector2(GetStretchedButtonWidthDelta(labelRect, toolButtonHorizontalPadding), Mathf.Max(labelRect.sizeDelta.y, toolButtonMinHeight));
        backgroundRect.pivot = labelRect.pivot;
        backgroundRect.localScale = Vector3.one;
        CopyRectTransform(labelRect, backgroundRect);

        Image image = backgroundObject.GetComponent<Image>();
        if (image == null)
            image = backgroundObject.AddComponent<Image>();

        image.color = toolButtonNormalColor;
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

    private float GetStretchedButtonWidthDelta(RectTransform rectTransform, float horizontalPadding)
    {
        if (rectTransform != null && !Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x))
            return -Mathf.Abs(horizontalPadding * 2f);

        return Mathf.Max(rectTransform != null ? rectTransform.sizeDelta.x : 0f, 72f);
    }

    private string GetRotateHintDisplayText()
    {
        if (string.IsNullOrEmpty(rotateHintText))
            return string.Empty;

        return rotateHintText.Contains("\n") ? rotateHintText : rotateHintText.Replace(" to ", "\nto ");
    }

    private void UpdatePlacementToolButtonVisuals()
    {
        if (placeModeButtonBackground != null)
            placeModeButtonBackground.color = currentToolMode == PlacementToolMode.Place ? toolButtonSelectedColor : toolButtonNormalColor;

        if (deleteModeButtonBackground != null)
            deleteModeButtonBackground.color = currentToolMode == PlacementToolMode.Delete ? toolButtonSelectedColor : toolButtonNormalColor;
    }

    private void SetupObjectSlots()
    {
        if (objectPlacementPanel == null)
            return;

        DestroySlotPreviews();

        Button[] buttons = objectPlacementPanel.GetComponentsInChildren<Button>(true);
        slotPrefabIndices = CreateSlotPrefabAssignments(buttons);

        for (int i = 0; i < buttons.Length; i++)
        {
            int slotIndex;
            if (!TryGetObjectSlotIndex(buttons[i].gameObject, out slotIndex))
                continue;

            int prefabIndex;
            bool hasPrefab = TryGetPrefabIndexForSlotIndex(slotIndex, out prefabIndex);
            buttons[i].interactable = hasPrefab;
            UpdateObjectSlotPreview(buttons[i].gameObject, slotIndex, prefabIndex, hasPrefab);
        }
    }

    private int[] CreateSlotPrefabAssignments(Button[] buttons)
    {
        int slotCount = GetObjectSlotCount(buttons);
        int[] assignments = new int[slotCount];
        for (int i = 0; i < assignments.Length; i++)
            assignments[i] = -1;

        List<int> validPrefabIndices = GetValidPrefabIndices();
        if (validPrefabIndices.Count == 0)
            return assignments;

        List<int> randomBag = new List<int>(validPrefabIndices);
        for (int slotIndex = 0; slotIndex < assignments.Length; slotIndex++)
        {
            if (!randomizeSlotsOnStart)
            {
                int prefabIndex;
                if (TryGetFallbackPrefabIndexForSlotIndex(slotIndex, out prefabIndex))
                    assignments[slotIndex] = prefabIndex;
                continue;
            }

            if (randomBag.Count == 0)
            {
                if (!repeatPrefabsForExtraSlots)
                    break;

                randomBag.AddRange(validPrefabIndices);
            }

            int randomBagIndex = Random.Range(0, randomBag.Count);
            assignments[slotIndex] = randomBag[randomBagIndex];
            randomBag.RemoveAt(randomBagIndex);
        }

        return assignments;
    }

    private int GetObjectSlotCount(Button[] buttons)
    {
        int maxSlotIndex = -1;
        for (int i = 0; i < buttons.Length; i++)
        {
            int slotIndex;
            if (TryGetObjectSlotIndex(buttons[i].gameObject, out slotIndex))
                maxSlotIndex = Mathf.Max(maxSlotIndex, slotIndex);
        }

        return maxSlotIndex + 1;
    }

    private List<int> GetValidPrefabIndices()
    {
        List<int> validPrefabIndices = new List<int>();
        if (placeablePrefabs == null)
            return validPrefabIndices;

        for (int i = 0; i < placeablePrefabs.Length; i++)
        {
            if (IsValidPrefabIndex(i))
                validPrefabIndices.Add(i);
        }

        return validPrefabIndices;
    }

    private bool TryGetPrefabIndexForSlotIndex(int slotIndex, out int prefabIndex)
    {
        prefabIndex = -1;

        if (slotIndex < 0 || placeablePrefabs == null || placeablePrefabs.Length == 0)
            return false;

        if (slotPrefabIndices != null &&
            slotIndex < slotPrefabIndices.Length &&
            IsValidPrefabIndex(slotPrefabIndices[slotIndex]))
        {
            prefabIndex = slotPrefabIndices[slotIndex];
            return true;
        }

        return TryGetFallbackPrefabIndexForSlotIndex(slotIndex, out prefabIndex);
    }

    private bool TryGetFallbackPrefabIndexForSlotIndex(int slotIndex, out int prefabIndex)
    {
        prefabIndex = -1;

        if (IsValidPrefabIndex(slotIndex))
        {
            prefabIndex = slotIndex;
            return true;
        }

        if (!repeatPrefabsForExtraSlots)
            return false;

        for (int i = 0; i < placeablePrefabs.Length; i++)
        {
            int candidateIndex = (slotIndex + i) % placeablePrefabs.Length;
            if (IsValidPrefabIndex(candidateIndex))
            {
                prefabIndex = candidateIndex;
                return true;
            }
        }

        return false;
    }

    private void UpdateObjectSlotPreview(GameObject slot, int slotIndex, int prefabIndex, bool hasPrefab)
    {
        if (slot == null)
            return;

        HideObjectSlotText(slot);

        if (!hasPrefab || !showSlotPrefabPreview)
            return;

        RawImage previewImage = EnsureSlotPreviewImage(slot);
        if (previewImage == null)
            return;

        int textureSize = Mathf.Max(32, slotPreviewTextureSize);
        RenderTexture texture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
        texture.name = slot.name + "_PreviewTexture";
        texture.Create();
        previewImage.texture = texture;

        GameObject worldObject = CreateSlotPreviewWorldObject(slotIndex, prefabIndex, texture);
        if (worldObject == null)
        {
            previewImage.texture = null;
            texture.Release();
            DestroySlotPreviewObject(texture);
            return;
        }

        slotPreviewBindings.Add(new SlotPreviewBinding
        {
            image = previewImage,
            texture = texture,
            worldObject = worldObject
        });
    }

    private void HideObjectSlotText(GameObject slot)
    {
        Text[] labels = slot.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].text = string.Empty;
            labels[i].gameObject.SetActive(false);
        }
    }

    private RawImage EnsureSlotPreviewImage(GameObject slot)
    {
        const string previewImageName = "ObjectSlotPrefabPreview";

        Transform existingPreview = slot.transform.Find(previewImageName);
        RawImage previewImage = existingPreview != null ? existingPreview.GetComponent<RawImage>() : null;

        if (previewImage == null)
        {
            GameObject previewObject = new GameObject(previewImageName);
            previewObject.transform.SetParent(slot.transform, false);
            previewImage = previewObject.AddComponent<RawImage>();
        }

        previewImage.raycastTarget = false;
        previewImage.color = Color.white;

        RectTransform rectTransform = previewImage.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(slotPreviewPadding, slotPreviewPadding);
        rectTransform.offsetMax = new Vector2(-slotPreviewPadding, -slotPreviewPadding);
        rectTransform.localScale = Vector3.one;
        rectTransform.SetAsLastSibling();

        AspectRatioFitter aspectRatioFitter = previewImage.GetComponent<AspectRatioFitter>();
        if (aspectRatioFitter == null)
            aspectRatioFitter = previewImage.gameObject.AddComponent<AspectRatioFitter>();

        aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectRatioFitter.aspectRatio = 1f;

        return previewImage;
    }

    private GameObject CreateSlotPreviewWorldObject(int slotIndex, int prefabIndex, RenderTexture texture)
    {
        if (!IsValidPrefabIndex(prefabIndex))
            return null;

        if (slotPreviewRoot == null)
            slotPreviewRoot = new GameObject("ObjectSlotPreviewRoot");

        Vector3 previewCenter = slotPreviewWorldOrigin + new Vector3(slotIndex * 8f, 0f, 0f);
        GameObject worldObject = new GameObject("ObjectSlotPreview_" + slotIndex);
        worldObject.transform.SetParent(slotPreviewRoot.transform, false);
        worldObject.transform.position = previewCenter;

        GameObject model = Instantiate(placeablePrefabs[prefabIndex], worldObject.transform);
        model.name = placeablePrefabs[prefabIndex].name + "_SlotPreview";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(20f, -35f, 0f);
        model.transform.localScale = placeablePrefabs[prefabIndex].transform.localScale;
        model.SetActive(true);
        DisableColliders(model);
        EnableRenderers(model);
        SetLayerRecursively(model, LayerMask.NameToLayer("Ignore Raycast"));

        Bounds bounds;
        if (!TryGetRendererBounds(model, out bounds))
        {
            DestroySlotPreviewObject(worldObject);
            return null;
        }

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        maxSize = Mathf.Max(0.5f, maxSize);

        Camera previewCamera = CreateSlotPreviewCamera(worldObject.transform, bounds, maxSize, texture);
        GameObject lightObject = new GameObject("SlotPreviewLight");
        lightObject.transform.SetParent(worldObject.transform, false);
        lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

        Light previewLight = lightObject.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.2f;

        previewCamera.Render();
        previewCamera.enabled = false;
        worldObject.SetActive(false);
        return worldObject;
    }

    private bool TryGetLocalRendererFootprintSize(GameObject target, out Vector2 size)
    {
        size = Vector2.one;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return false;

        Bounds localBounds = new Bounds();
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds rendererBounds = renderers[i].bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z
                        );

                        Vector3 localCorner = target.transform.InverseTransformPoint(corner);
                        if (!hasBounds)
                        {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        if (!hasBounds)
            return false;

        size = new Vector2(Mathf.Abs(localBounds.size.x), Mathf.Abs(localBounds.size.z));
        return true;
    }

    private Camera CreateSlotPreviewCamera(Transform parent, Bounds bounds, float maxSize, RenderTexture texture)
    {
        GameObject cameraObject = new GameObject("SlotPreviewCamera");
        cameraObject.transform.SetParent(parent, false);

        Camera previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = slotPreviewBackgroundColor;
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = maxSize * 0.8f;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = maxSize * 10f;
        previewCamera.targetTexture = texture;
        previewCamera.enabled = true;

        cameraObject.transform.position = bounds.center + new Vector3(0f, maxSize * 0.45f, -maxSize * 3f);
        cameraObject.transform.LookAt(bounds.center);
        return previewCamera;
    }

    private bool TryGetRendererBounds(GameObject target, out Bounds bounds)
    {
        bounds = new Bounds(target.transform.position, Vector3.one);

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return false;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    private void DestroySlotPreviews()
    {
        for (int i = 0; i < slotPreviewBindings.Count; i++)
        {
            SlotPreviewBinding binding = slotPreviewBindings[i];
            if (binding.image != null)
                binding.image.texture = null;

            if (binding.texture != null)
            {
                binding.texture.Release();
                DestroySlotPreviewObject(binding.texture);
            }
        }

        slotPreviewBindings.Clear();

        if (slotPreviewRoot != null)
        {
            DestroySlotPreviewObject(slotPreviewRoot);
            slotPreviewRoot = null;
        }
    }

    private void DestroySlotPreviewObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private bool TryGetObjectSlotIndex(GameObject target, out int slotIndex)
    {
        slotIndex = -1;

        const string slotPrefix = "ObjectSlot_";
        if (target == null || !target.name.StartsWith(slotPrefix))
            return false;

        int slotNumber;
        if (!int.TryParse(target.name.Substring(slotPrefix.Length), out slotNumber))
            return false;

        slotIndex = slotNumber - 1;
        return true;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (layer == -1)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
