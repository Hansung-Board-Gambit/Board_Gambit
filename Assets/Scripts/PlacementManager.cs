using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlacementManager : MonoBehaviour
{
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
    public GameObject[] placeablePrefabs;

    private int selectedIndex = -1;
    private GameObject previewObject;

    private void Start()
    {
        EnsureObjectSlotButtonsMatchPrefabs();
        UpdatePointText();
    }

    private void Update()
    {
        if (objectPlacementPanel == null || !objectPlacementPanel.activeInHierarchy)
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
        Debug.Log("SelectObject called, index = " + index);

        if (!IsValidPrefabIndex(index))
        {
            CancelSelection();
            Debug.LogWarning("SelectObject ignored because no prefab is assigned at index = " + index);
            return;
        }

        selectedIndex = index;
        DestroyPreview();
        EnsurePreviewExists();
    }

    public void CancelSelection()
    {
        selectedIndex = -1;
        DestroyPreview();
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

        bool insideBoard =
            position.x - halfX >= bounds.min.x &&
            position.x + halfX <= bounds.max.x &&
            position.z - halfZ >= bounds.min.z &&
            position.z + halfZ <= bounds.max.z;

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

        GameObject prefab = placeablePrefabs[selectedIndex];
        GameObject placed = Instantiate(prefab);
        if (placed == null)
            return;

        int placedCount = dataStore.placedObjects != null ? dataStore.placedObjects.Count + 1 : 1;
        placed.name = prefab.name + "_Placed_" + placedCount;

        if (placedParent != null)
            placed.transform.SetParent(placedParent, true);

        placed.transform.position = position;
        placed.transform.rotation = Quaternion.identity;
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

        if (!dataStore.CanSpendPoint())
            CancelSelection();
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

    private void EnsureObjectSlotButtonsMatchPrefabs()
    {
        if (objectPlacementPanel == null)
            return;

        Button[] buttons = objectPlacementPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            int prefabIndex;
            if (!TryGetObjectSlotPrefabIndex(buttons[i].gameObject, out prefabIndex))
                continue;

            buttons[i].interactable = IsValidPrefabIndex(prefabIndex);
        }
    }

    private bool TryGetObjectSlotPrefabIndex(GameObject target, out int prefabIndex)
    {
        prefabIndex = -1;

        const string slotPrefix = "ObjectSlot_";
        if (target == null || !target.name.StartsWith(slotPrefix))
            return false;

        int slotNumber;
        if (!int.TryParse(target.name.Substring(slotPrefix.Length), out slotNumber))
            return false;

        prefabIndex = slotNumber - 1;
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
