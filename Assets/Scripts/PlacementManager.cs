using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlacementManager : MonoBehaviour
{
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
    public GameObject[] placeablePrefabs;

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

    [Header("Editor Test")]
    public bool allowEditorLocalTest = false;

    private int selectedIndex = -1;
    private GameObject previewObject;
    private int[] slotPrefabIndices;
    private GameObject slotPreviewRoot;
    private readonly List<SlotPreviewBinding> slotPreviewBindings = new List<SlotPreviewBinding>();

    private void OnEnable()
    {
        LobbyState.PrepObjectPlaced += HandleNetworkObjectPlaced;
    }

    private void OnDisable()
    {
        LobbyState.PrepObjectPlaced -= HandleNetworkObjectPlaced;
    }

    private void OnDestroy()
    {
        DestroySlotPreviews();
    }

    private void Start()
    {
        RefreshObjectSlots();
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

        if (!HasValidSelection())
        {
            SetPreviewActive(false);
            return;
        }

        if (IsPointerBlockedByUi())
            return;

        Vector3 snappedPosition;
        if (!TryGetSnappedBoardPoint(out snappedPosition))
        {
            SetPreviewActive(false);
            return;
        }

        EnsurePreviewExists();
        if (previewObject == null)
            return;

        PlaceableObject previewInfo = previewObject.GetComponent<PlaceableObject>();
        snappedPosition.y = GetBoardTopY() + (previewInfo != null ? previewInfo.yOffset : 0f);

        bool canPlace = CanPlace(snappedPosition, previewInfo);
        if (!canPlace)
        {
            SetPreviewActive(false);
            return;
        }

        previewObject.transform.position = snappedPosition;
        previewObject.SetActive(true);
        SetPreviewColor(new Color(0f, 1f, 0f, 0.5f));

        if (Input.GetMouseButtonDown(0))
        {
            PlaceSelectedObject(snappedPosition);
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

        Debug.Log("SelectObject called, slot index = " + index + ", prefab index = " + prefabIndex);

        selectedIndex = prefabIndex;
        DestroyPreview();
        EnsurePreviewExists();
    }

    public void CancelSelection()
    {
        selectedIndex = -1;
        DestroyPreview();
    }

    public void RefreshObjectSlots()
    {
        SetupObjectSlots();
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

        DisableColliders(previewObject);
        SetLayerRecursively(previewObject, LayerMask.NameToLayer("Ignore Raycast"));
    }

    private bool CanPlace(Vector3 position, PlaceableObject info)
    {
        if (dataStore == null || !dataStore.CanSpendPoint())
            return false;

        if (boardCollider == null)
            return false;

        Vector2 footprint = Vector2.one;
        if (info != null)
            footprint = info.footprint;

        Bounds bounds = boardCollider.bounds;

        float halfX = footprint.x * 0.5f;
        float halfZ = footprint.y * 0.5f;

        float edgeMargin = GetBlockedEdgeMargin();
        bool insideBoard =
            position.x - halfX >= bounds.min.x + edgeMargin &&
            position.x + halfX <= bounds.max.x - edgeMargin &&
            position.z - halfZ >= bounds.min.z + edgeMargin &&
            position.z + halfZ <= bounds.max.z - edgeMargin;

        if (!insideBoard)
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

    private void PlaceSelectedObject(Vector3 position)
    {
        if (dataStore == null || !HasValidSelection())
            return;

        int prefabIndex = selectedIndex;
        CancelSelection();

        if (LobbyState.Instance != null)
        {
            LobbyState.Instance.RequestPlacePrepObject(prefabIndex, position, Quaternion.identity);
            return;
        }

        CreatePlacedObject(prefabIndex, position, Quaternion.identity);
    }

    private void HandleNetworkObjectPlaced(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        CreatePlacedObject(prefabIndex, position, rotation);
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

        placed.transform.position = position;
        placed.transform.rotation = rotation;
        placed.transform.localScale = prefab.transform.localScale;
        placed.SetActive(true);

        int enabledRenderers = EnableRenderers(placed);
        int enabledColliders = EnableColliders(placed);
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

        PlaceableObject info = placed.GetComponent<PlaceableObject>();
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
            ", materialsFixed=" + fixedMaterials
        );

        SetPreviewActive(false);
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

    private float GetBlockedEdgeMargin()
    {
        float safeGridSize = Mathf.Max(0.01f, Mathf.Abs(gridSize));
        return Mathf.Max(0, blockedEdgeCellCount) * safeGridSize;
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
}
