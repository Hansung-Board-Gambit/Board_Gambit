using UnityEngine;

public class BoardGridRenderer : MonoBehaviour
{
    [Header("References")]
    public Collider boardCollider;
    public GameObject[] visibleWhilePanelsActive;
    public bool visibleWhenNoPanelsAssigned = true;

    [Header("Grid")]
    public float gridSize = 1f;
    public float yOffset = 0.03f;
    public Color lineColor = new Color(1f, 1f, 1f, 0.35f);
    public float lineWidth = 0.03f;
    public float lineHeight = 0.02f;
    public bool drawCenterLines = true;
    public Color centerLineColor = new Color(1f, 1f, 1f, 0.35f);
    public float centerLineWidthMultiplier = 2f;
    public float centerLineYOffset = 0.12f;
    public bool useCircularBoardBounds = false;
    public string gridRootName = "BoardGridLines";

    private Material lineMaterial;
    private Material centerLineMaterial;
    private GameObject gridRootObject;

    private void Start()
    {
        RebuildGrid();
    }

    private void Update()
    {
        UpdateGridVisibility();
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(lineMaterial);
            else
                DestroyImmediate(lineMaterial);
        }

        if (centerLineMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(centerLineMaterial);
            else
                DestroyImmediate(centerLineMaterial);
        }
    }

    [ContextMenu("Rebuild Grid")]
    public void RebuildGrid()
    {
        if (boardCollider == null)
            return;

        ClearGrid();

        Transform gridRoot = new GameObject(gridRootName).transform;
        gridRoot.SetParent(transform, false);
        gridRootObject = gridRoot.gameObject;

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer != -1)
            gridRoot.gameObject.layer = ignoreRaycastLayer;

        Bounds bounds = boardCollider.bounds;
        float safeGridSize = Mathf.Max(0.01f, Mathf.Abs(gridSize));
        float y = bounds.max.y + yOffset;

        float minX = Mathf.Ceil(bounds.min.x / safeGridSize) * safeGridSize;
        float maxX = Mathf.Floor(bounds.max.x / safeGridSize) * safeGridSize;
        float minZ = Mathf.Ceil(bounds.min.z / safeGridSize) * safeGridSize;
        float maxZ = Mathf.Floor(bounds.max.z / safeGridSize) * safeGridSize;

        float width = Mathf.Max(0.001f, lineWidth);
        float height = Mathf.Max(0.001f, lineHeight);
        Material material = GetLineMaterial();
        Material centerMaterial = material;
        float centerWidth = width;
        float centerX = SnapToGrid(bounds.center.x, safeGridSize);
        float centerZ = SnapToGrid(bounds.center.z, safeGridSize);
        bool drawCenterX = drawCenterLines && centerX >= minX - 0.001f && centerX <= maxX + 0.001f;
        bool drawCenterZ = drawCenterLines && centerZ >= minZ - 0.001f && centerZ <= maxZ + 0.001f;

        for (float x = minX; x <= maxX + 0.001f; x += safeGridSize)
        {
            if (drawCenterX && Mathf.Abs(x - centerX) <= 0.001f)
                continue;

            float zLength = bounds.size.z;
            if (useCircularBoardBounds && !TryGetCircleLineLength(x, bounds.center.x, GetBoardRadius(bounds), out zLength))
                continue;

            CreateGridBar(
                gridRoot,
                "GridLine_X",
                new Vector3(x, y, bounds.center.z),
                new Vector3(width, height, zLength),
                material
            );
        }

        for (float z = minZ; z <= maxZ + 0.001f; z += safeGridSize)
        {
            if (drawCenterZ && Mathf.Abs(z - centerZ) <= 0.001f)
                continue;

            float xLength = bounds.size.x;
            if (useCircularBoardBounds && !TryGetCircleLineLength(z, bounds.center.z, GetBoardRadius(bounds), out xLength))
                continue;

            CreateGridBar(
                gridRoot,
                "GridLine_Z",
                new Vector3(bounds.center.x, y, z),
                new Vector3(xLength, height, width),
                material
            );
        }

        float centerLineY = y;
        if (drawCenterX)
        {
            float zLength = bounds.size.z;
            if (useCircularBoardBounds)
                TryGetCircleLineLength(centerX, bounds.center.x, GetBoardRadius(bounds), out zLength);

            CreateGridBar(
                gridRoot,
                "GridLine_CenterX",
                new Vector3(centerX, centerLineY, bounds.center.z),
                new Vector3(centerWidth, height, zLength),
                centerMaterial
            );
        }

        if (drawCenterZ)
        {
            float xLength = bounds.size.x;
            if (useCircularBoardBounds)
                TryGetCircleLineLength(centerZ, bounds.center.z, GetBoardRadius(bounds), out xLength);

            CreateGridBar(
                gridRoot,
                "GridLine_CenterZ",
                new Vector3(bounds.center.x, centerLineY, centerZ),
                new Vector3(xLength, height, centerWidth),
                centerMaterial
            );
        }

        UpdateGridVisibility();
    }

    private void ClearGrid()
    {
        Transform existingGrid = transform.Find(gridRootName);
        if (existingGrid == null)
            return;

        if (Application.isPlaying)
            Destroy(existingGrid.gameObject);
        else
            DestroyImmediate(existingGrid.gameObject);

        gridRootObject = null;
    }

    private void UpdateGridVisibility()
    {
        if (gridRootObject == null)
            return;

        gridRootObject.SetActive(ShouldShowGrid());
    }

    private bool ShouldShowGrid()
    {
        if (visibleWhilePanelsActive == null || visibleWhilePanelsActive.Length == 0)
            return visibleWhenNoPanelsAssigned;

        for (int i = 0; i < visibleWhilePanelsActive.Length; i++)
        {
            if (visibleWhilePanelsActive[i] != null && visibleWhilePanelsActive[i].activeInHierarchy)
                return true;
        }

        return false;
    }

    private void CreateGridBar(Transform parent, string lineName, Vector3 center, Vector3 scale, Material material)
    {
        GameObject lineObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lineObject.name = lineName;
        lineObject.transform.SetParent(parent, false);
        lineObject.transform.position = center;
        lineObject.transform.localScale = scale;

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer != -1)
            lineObject.layer = ignoreRaycastLayer;

        Collider lineCollider = lineObject.GetComponent<Collider>();
        if (lineCollider != null)
            DestroyGeneratedObject(lineCollider);

        MeshRenderer renderer = lineObject.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
    }

    private Material GetLineMaterial()
    {
        if (lineMaterial != null)
            return lineMaterial;

        lineMaterial = CreateLineMaterial(GetVisibleLineColor(lineColor));
        if (lineMaterial != null)
            lineMaterial.name = "BoardGridLine_Runtime";

        return lineMaterial;
    }

    private Material GetCenterLineMaterial()
    {
        if (centerLineMaterial != null)
            return centerLineMaterial;

        centerLineMaterial = CreateLineMaterial(GetVisibleLineColor(centerLineColor));
        if (centerLineMaterial != null)
            centerLineMaterial.name = "BoardGridCenterLine_Runtime";

        return centerLineMaterial;
    }

    private Material CreateLineMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Hidden/Internal-Colored");

        if (shader == null)
            return null;

        Material material = new Material(shader);
        SetMaterialColor(material, color);
        return material;
    }

    private Color GetVisibleLineColor(Color sourceColor)
    {
        float brightness = Mathf.Clamp01(sourceColor.a);
        if (brightness <= 0f)
            brightness = 1f;

        return new Color(sourceColor.r * brightness, sourceColor.g * brightness, sourceColor.b * brightness, 1f);
    }

    private float SnapToGrid(float value, float safeGridSize)
    {
        return Mathf.Round(value / safeGridSize) * safeGridSize;
    }

    private float GetBoardRadius(Bounds bounds)
    {
        return Mathf.Min(bounds.extents.x, bounds.extents.z);
    }

    private bool TryGetCircleLineLength(float linePosition, float circleCenterOnLineAxis, float radius, out float length)
    {
        length = 0f;

        float distanceFromCenter = Mathf.Abs(linePosition - circleCenterOnLineAxis);
        if (distanceFromCenter > radius)
            return false;

        float halfLength = Mathf.Sqrt(Mathf.Max(0f, radius * radius - distanceFromCenter * distanceFromCenter));
        length = Mathf.Max(0.001f, halfLength * 2f);
        return true;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private void DestroyGeneratedObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
