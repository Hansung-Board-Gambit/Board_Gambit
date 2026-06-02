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
    public float wallHeight = 6f;
    public int segments = 128;

    [Header("Material Mapping")]
    public float textureRepeatPerMeter = 1f;

    [Header("Invisible Top Collider")]
    public bool createInvisibleTopCollider = true;
    public float invisibleTopHeight = 14f;

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
        Vector3[] vertices = new Vector3[safeSegments * 16];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[safeSegments * 24];
        float repeat = Mathf.Max(0.01f, textureRepeatPerMeter);
        float heightUv = height * repeat;
        float thicknessUv = thickness * repeat;

        for (int i = 0; i < safeSegments; i++)
        {
            int next = (i + 1) % safeSegments;
            float angle = (Mathf.PI * 2f * i) / safeSegments;
            float nextAngle = (Mathf.PI * 2f * next) / safeSegments;
            float innerU = innerRadius * angle * repeat;
            float nextInnerU = innerRadius * nextAngle * repeat;
            float outerU = outerRadius * angle * repeat;
            float nextOuterU = outerRadius * nextAngle * repeat;

            if (next == 0)
            {
                nextInnerU = innerRadius * Mathf.PI * 2f * repeat;
                nextOuterU = outerRadius * Mathf.PI * 2f * repeat;
            }

            Vector3 inner = new Vector3(Mathf.Cos(angle) * innerRadius, 0f, Mathf.Sin(angle) * innerRadius) + center;
            Vector3 nextInner = new Vector3(Mathf.Cos(nextAngle) * innerRadius, 0f, Mathf.Sin(nextAngle) * innerRadius) + center;
            Vector3 outer = new Vector3(Mathf.Cos(angle) * outerRadius, 0f, Mathf.Sin(angle) * outerRadius) + center;
            Vector3 nextOuter = new Vector3(Mathf.Cos(nextAngle) * outerRadius, 0f, Mathf.Sin(nextAngle) * outerRadius) + center;

            Vector3 innerBottom = transform.InverseTransformPoint(new Vector3(inner.x, bottomY, inner.z));
            Vector3 innerTop = transform.InverseTransformPoint(new Vector3(inner.x, bottomY + height, inner.z));
            Vector3 nextInnerBottom = transform.InverseTransformPoint(new Vector3(nextInner.x, bottomY, nextInner.z));
            Vector3 nextInnerTop = transform.InverseTransformPoint(new Vector3(nextInner.x, bottomY + height, nextInner.z));
            Vector3 outerBottom = transform.InverseTransformPoint(new Vector3(outer.x, bottomY, outer.z));
            Vector3 outerTop = transform.InverseTransformPoint(new Vector3(outer.x, bottomY + height, outer.z));
            Vector3 nextOuterBottom = transform.InverseTransformPoint(new Vector3(nextOuter.x, bottomY, nextOuter.z));
            Vector3 nextOuterTop = transform.InverseTransformPoint(new Vector3(nextOuter.x, bottomY + height, nextOuter.z));

            int v = i * 16;
            int t = i * 24;

            SetQuad(vertices, uv, triangles, v, t,
                innerBottom, nextInnerBottom, nextInnerTop, innerTop,
                new Vector2(innerU, 0f), new Vector2(nextInnerU, 0f), new Vector2(nextInnerU, heightUv), new Vector2(innerU, heightUv));

            SetQuad(vertices, uv, triangles, v + 4, t + 6,
                outerBottom, outerTop, nextOuterTop, nextOuterBottom,
                new Vector2(outerU, 0f), new Vector2(outerU, heightUv), new Vector2(nextOuterU, heightUv), new Vector2(nextOuterU, 0f));

            SetQuad(vertices, uv, triangles, v + 8, t + 12,
                innerTop, nextInnerTop, nextOuterTop, outerTop,
                new Vector2(innerU, 0f), new Vector2(nextInnerU, 0f), new Vector2(nextOuterU, thicknessUv), new Vector2(outerU, thicknessUv));

            SetQuad(vertices, uv, triangles, v + 12, t + 18,
                innerBottom, outerBottom, nextOuterBottom, nextInnerBottom,
                new Vector2(innerU, 0f), new Vector2(outerU, thicknessUv), new Vector2(nextOuterU, thicknessUv), new Vector2(nextInnerU, 0f));
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void SetQuad(
        Vector3[] vertices,
        Vector2[] uv,
        int[] triangles,
        int vertexIndex,
        int triangleIndex,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Vector2 uvA,
        Vector2 uvB,
        Vector2 uvC,
        Vector2 uvD)
    {
        vertices[vertexIndex] = a;
        vertices[vertexIndex + 1] = b;
        vertices[vertexIndex + 2] = c;
        vertices[vertexIndex + 3] = d;

        uv[vertexIndex] = uvA;
        uv[vertexIndex + 1] = uvB;
        uv[vertexIndex + 2] = uvC;
        uv[vertexIndex + 3] = uvD;

        triangles[triangleIndex] = vertexIndex;
        triangles[triangleIndex + 1] = vertexIndex + 1;
        triangles[triangleIndex + 2] = vertexIndex + 2;
        triangles[triangleIndex + 3] = vertexIndex;
        triangles[triangleIndex + 4] = vertexIndex + 2;
        triangles[triangleIndex + 5] = vertexIndex + 3;
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
