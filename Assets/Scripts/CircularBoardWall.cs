using UnityEngine;

[ExecuteAlways]
public class CircularBoardWall : MonoBehaviour
{
    private const string InvisibleTopLayerName = "UI";

    [Header("References")]
    public Collider boardCollider;
    public Material wallMaterial;
    public GameObject[] squareWallsToHide;

    [Header("Wall Shape")]
    public float radiusOffset = 0.25f;
    public float wallThickness = 0.5f;
    public float wallHeight = 4f;
    public int segments = 128;

    [Header("Invisible Top Collider")]
    public bool createInvisibleTopCollider = true;
    public float invisibleTopHeight = 8f;

    [Header("Generated Objects")]
    public string generatedRootName = "CircularBoardWall_Generated";
    public Color fallbackWallColor = new Color(0.14f, 0.42f, 0.75f, 1f);

    private GameObject generatedRoot;
    private Material runtimeMaterial;
    private float lastRadius;
    private float lastThickness;
    private float lastHeight;
    private float lastInvisibleTopHeight;
    private int lastSegments;

    private void OnEnable()
    {
        HideSquareWalls();
        RebuildIfNeeded(true);
    }

    private void Start()
    {
        HideSquareWalls();
        RebuildIfNeeded(true);
    }

    private void Update()
    {
        HideSquareWalls();
        RebuildIfNeeded(false);
    }

    private void OnValidate()
    {
        radiusOffset = Mathf.Max(0f, radiusOffset);
        wallThickness = Mathf.Max(0.05f, wallThickness);
        wallHeight = Mathf.Max(0.1f, wallHeight);
        invisibleTopHeight = Mathf.Max(0f, invisibleTopHeight);
        segments = Mathf.Clamp(segments, 16, 256);
        RebuildIfNeeded(true);
    }

    private void OnDestroy()
    {
        DestroyGeneratedObject(generatedRoot);

        if (runtimeMaterial != null)
            DestroyGeneratedObject(runtimeMaterial);
    }

    [ContextMenu("Rebuild Circular Wall")]
    public void Rebuild()
    {
        RebuildIfNeeded(true);
    }

    private void RebuildIfNeeded(bool force)
    {
        if (boardCollider == null)
            return;

        Bounds bounds = boardCollider.bounds;
        float boardRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);
        float innerRadius = boardRadius + radiusOffset;
        int safeSegments = Mathf.Clamp(segments, 16, 256);

        if (!force &&
            generatedRoot != null &&
            Mathf.Approximately(lastRadius, innerRadius) &&
            Mathf.Approximately(lastThickness, wallThickness) &&
            Mathf.Approximately(lastHeight, wallHeight) &&
            Mathf.Approximately(lastInvisibleTopHeight, invisibleTopHeight) &&
            lastSegments == safeSegments)
        {
            return;
        }

        DestroyGeneratedObject(generatedRoot);

        generatedRoot = new GameObject(generatedRootName);
        generatedRoot.hideFlags = HideFlags.DontSave;
        generatedRoot.transform.SetParent(transform, false);
        generatedRoot.transform.position = Vector3.zero;
        generatedRoot.transform.rotation = Quaternion.identity;
        generatedRoot.transform.localScale = Vector3.one;

        CreateVisibleWall(bounds, innerRadius, safeSegments);

        if (createInvisibleTopCollider && invisibleTopHeight > 0f)
            CreateInvisibleTopCollider(bounds, innerRadius, safeSegments);

        lastRadius = innerRadius;
        lastThickness = wallThickness;
        lastHeight = wallHeight;
        lastInvisibleTopHeight = invisibleTopHeight;
        lastSegments = safeSegments;
    }

    private void CreateVisibleWall(Bounds bounds, float innerRadius, int safeSegments)
    {
        GameObject wallObject = new GameObject("CircularBoardWall");
        wallObject.hideFlags = HideFlags.DontSave;
        wallObject.transform.SetParent(generatedRoot.transform, false);

        Mesh mesh = BuildRingMesh(bounds, innerRadius, wallThickness, wallHeight, safeSegments, bounds.max.y);

        MeshFilter meshFilter = wallObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = wallObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = GetWallMaterial();

        MeshCollider meshCollider = wallObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private void CreateInvisibleTopCollider(Bounds bounds, float innerRadius, int safeSegments)
    {
        GameObject topObject = new GameObject("CircularBoardWall_InvisibleTop");
        topObject.hideFlags = HideFlags.DontSave;
        topObject.transform.SetParent(generatedRoot.transform, false);
        SetLayerIfExists(topObject, InvisibleTopLayerName);

        Mesh mesh = BuildRingMesh(bounds, innerRadius, wallThickness, invisibleTopHeight, safeSegments, bounds.max.y + wallHeight);

        MeshCollider meshCollider = topObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private Mesh BuildRingMesh(Bounds bounds, float innerRadius, float thickness, float height, int safeSegments, float bottomY)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Generated Circular Board Wall";
        mesh.hideFlags = HideFlags.DontSave;

        float outerRadius = innerRadius + thickness;
        Vector3 center = new Vector3(bounds.center.x, 0f, bounds.center.z);
        Vector3[] vertices = new Vector3[safeSegments * 4];
        int[] triangles = new int[safeSegments * 24];

        for (int i = 0; i < safeSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / safeSegments;
            Vector3 inner = new Vector3(Mathf.Cos(angle) * innerRadius, 0f, Mathf.Sin(angle) * innerRadius) + center;
            Vector3 outer = new Vector3(Mathf.Cos(angle) * outerRadius, 0f, Mathf.Sin(angle) * outerRadius) + center;

            int vertexIndex = i * 4;
            vertices[vertexIndex] = transform.InverseTransformPoint(new Vector3(inner.x, bottomY, inner.z));
            vertices[vertexIndex + 1] = transform.InverseTransformPoint(new Vector3(inner.x, bottomY + height, inner.z));
            vertices[vertexIndex + 2] = transform.InverseTransformPoint(new Vector3(outer.x, bottomY, outer.z));
            vertices[vertexIndex + 3] = transform.InverseTransformPoint(new Vector3(outer.x, bottomY + height, outer.z));
        }

        for (int i = 0; i < safeSegments; i++)
        {
            int next = (i + 1) % safeSegments;
            int v = i * 4;
            int n = next * 4;
            int t = i * 24;

            AddQuad(triangles, t, v, n, n + 1, v + 1);
            AddQuad(triangles, t + 6, v + 2, v + 3, n + 3, n + 2);
            AddQuad(triangles, t + 12, v + 1, n + 1, n + 3, v + 3);
            AddQuad(triangles, t + 18, v, v + 2, n + 2, n);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void AddQuad(int[] triangles, int index, int a, int b, int c, int d)
    {
        triangles[index] = a;
        triangles[index + 1] = b;
        triangles[index + 2] = c;
        triangles[index + 3] = a;
        triangles[index + 4] = c;
        triangles[index + 5] = d;
    }

    private Material GetWallMaterial()
    {
        if (wallMaterial != null)
            return wallMaterial;

        if (runtimeMaterial != null)
            return runtimeMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            return null;

        runtimeMaterial = new Material(shader);
        runtimeMaterial.hideFlags = HideFlags.DontSave;
        runtimeMaterial.name = "CircularBoardWall_Runtime";

        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", fallbackWallColor);

        if (runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.SetColor("_Color", fallbackWallColor);

        return runtimeMaterial;
    }

    private void HideSquareWalls()
    {
        if (squareWallsToHide == null)
            return;

        for (int i = 0; i < squareWallsToHide.Length; i++)
        {
            if (squareWallsToHide[i] != null && squareWallsToHide[i].activeSelf)
                squareWallsToHide[i].SetActive(false);
        }
    }

    private void SetLayerIfExists(GameObject target, string layerName)
    {
        if (target == null)
            return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer != -1)
            target.layer = layer;
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
