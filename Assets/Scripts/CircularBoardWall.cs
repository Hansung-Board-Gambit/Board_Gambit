using UnityEngine;

[ExecuteAlways]
public class CircularBoardWall : MonoBehaviour
{
    private const string InvisibleTopLayerName = "UI";
    private const string VisibleWallObjectName = "CircularBoardWall";
    private const string LowerWallObjectName = "CircularBoardWall_Lower";
    private const string MiddleWallObjectName = "CircularBoardWall_Middle";
    private const string UpperWallObjectName = "CircularBoardWall_Upper";
    private const string InvisibleTopObjectName = "CircularBoardWall_InvisibleTop";

    [Header("References")]
    public Collider boardCollider;
    public Material wallMaterial;
    public GameObject[] squareWallsToHide;

    [Header("Wall Shape")]
    public float radiusOffset = 0.25f;
    public float wallThickness = 0.5f;
    public float wallHeight = 6f;
    public int segments = 128;

    [Header("Layered Visible Wall")]
    public bool useLayeredVisibleWall;
    public float lowerWallHeight = 1.5f;
    public float middleWallHeight = 8f;
    public float upperWallHeight = 1.5f;
    public Material lowerWallMaterial;
    public Material middleWallMaterial;
    public Material upperWallMaterial;

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
    private bool lastUseLayeredVisibleWall;
    private float lastLowerWallHeight;
    private float lastMiddleWallHeight;
    private float lastUpperWallHeight;
    private float lastInvisibleTopHeight;
    private float lastTextureRepeatPerMeter;
    private int lastSegments;
    private Material lastWallMaterial;
    private Material lastLowerWallMaterial;
    private Material lastMiddleWallMaterial;
    private Material lastUpperWallMaterial;

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
        lowerWallHeight = Mathf.Max(0.1f, lowerWallHeight);
        middleWallHeight = Mathf.Max(0.1f, middleWallHeight);
        upperWallHeight = Mathf.Max(0.1f, upperWallHeight);
        textureRepeatPerMeter = Mathf.Max(0.01f, textureRepeatPerMeter);
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
        float safeTextureRepeat = Mathf.Max(0.01f, textureRepeatPerMeter);

        if (!force &&
            generatedRoot != null &&
            Mathf.Approximately(lastRadius, innerRadius) &&
            Mathf.Approximately(lastThickness, wallThickness) &&
            Mathf.Approximately(lastHeight, wallHeight) &&
            lastUseLayeredVisibleWall == useLayeredVisibleWall &&
            Mathf.Approximately(lastLowerWallHeight, lowerWallHeight) &&
            Mathf.Approximately(lastMiddleWallHeight, middleWallHeight) &&
            Mathf.Approximately(lastUpperWallHeight, upperWallHeight) &&
            Mathf.Approximately(lastInvisibleTopHeight, invisibleTopHeight) &&
            Mathf.Approximately(lastTextureRepeatPerMeter, safeTextureRepeat) &&
            lastSegments == safeSegments)
        {
            if (lastWallMaterial != wallMaterial ||
                lastLowerWallMaterial != lowerWallMaterial ||
                lastMiddleWallMaterial != middleWallMaterial ||
                lastUpperWallMaterial != upperWallMaterial)
            {
                ApplyWallMaterialsToGeneratedWalls();
            }

            return;
        }

        generatedRoot = GetOrCreateGeneratedRoot();

        if (useLayeredVisibleWall)
            CreateOrUpdateLayeredVisibleWalls(bounds, innerRadius, safeSegments);
        else
            CreateOrUpdateVisibleWall(bounds, innerRadius, safeSegments);

        if (createInvisibleTopCollider && invisibleTopHeight > 0f)
            CreateOrUpdateInvisibleTopCollider(bounds, innerRadius, safeSegments);
        else
            DestroyGeneratedChild(InvisibleTopObjectName);

        lastRadius = innerRadius;
        lastThickness = wallThickness;
        lastHeight = wallHeight;
        lastUseLayeredVisibleWall = useLayeredVisibleWall;
        lastLowerWallHeight = lowerWallHeight;
        lastMiddleWallHeight = middleWallHeight;
        lastUpperWallHeight = upperWallHeight;
        lastInvisibleTopHeight = invisibleTopHeight;
        lastTextureRepeatPerMeter = safeTextureRepeat;
        lastSegments = safeSegments;
        lastWallMaterial = wallMaterial;
        lastLowerWallMaterial = lowerWallMaterial;
        lastMiddleWallMaterial = middleWallMaterial;
        lastUpperWallMaterial = upperWallMaterial;
    }

    private GameObject GetOrCreateGeneratedRoot()
    {
        if (generatedRoot != null)
            return generatedRoot;

        Transform existingRoot = transform.Find(generatedRootName);
        if (existingRoot != null)
        {
            generatedRoot = existingRoot.gameObject;
            return generatedRoot;
        }

        generatedRoot = new GameObject(generatedRootName);
        generatedRoot.hideFlags = HideFlags.DontSave;
        generatedRoot.transform.SetParent(transform, false);
        generatedRoot.transform.position = Vector3.zero;
        generatedRoot.transform.rotation = Quaternion.identity;
        generatedRoot.transform.localScale = Vector3.one;
        return generatedRoot;
    }

    private GameObject GetOrCreateGeneratedChild(string childName)
    {
        GameObject root = GetOrCreateGeneratedRoot();
        Transform existingChild = root.transform.Find(childName);
        if (existingChild != null)
            return existingChild.gameObject;

        GameObject child = new GameObject(childName);
        child.hideFlags = HideFlags.DontSave;
        child.transform.SetParent(root.transform, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child;
    }

    private void CreateOrUpdateVisibleWall(Bounds bounds, float innerRadius, int safeSegments)
    {
        DestroyGeneratedChild(LowerWallObjectName);
        DestroyGeneratedChild(MiddleWallObjectName);
        DestroyGeneratedChild(UpperWallObjectName);

        GameObject wallObject = GetOrCreateGeneratedChild(VisibleWallObjectName);
        wallObject.transform.SetParent(generatedRoot.transform, false);

        Mesh mesh = BuildRingMesh(bounds, innerRadius, wallThickness, wallHeight, safeSegments, bounds.max.y);

        MeshFilter meshFilter = GetOrAddComponent<MeshFilter>(wallObject);

        MeshRenderer meshRenderer = GetOrAddComponent<MeshRenderer>(wallObject);
        if (wallMaterial != null || meshRenderer.sharedMaterial == null)
            meshRenderer.sharedMaterial = GetWallMaterial();

        MeshCollider meshCollider = GetOrAddComponent<MeshCollider>(wallObject);
        ReplaceSharedMesh(meshFilter, meshCollider, mesh);
    }

    private void CreateOrUpdateLayeredVisibleWalls(Bounds bounds, float innerRadius, int safeSegments)
    {
        DestroyGeneratedChild(VisibleWallObjectName);

        float bottomY = bounds.max.y;
        CreateOrUpdateVisibleWallLayer(
            LowerWallObjectName,
            bounds,
            innerRadius,
            safeSegments,
            bottomY,
            lowerWallHeight,
            lowerWallMaterial);

        bottomY += lowerWallHeight;
        CreateOrUpdateVisibleWallLayer(
            MiddleWallObjectName,
            bounds,
            innerRadius,
            safeSegments,
            bottomY,
            middleWallHeight,
            middleWallMaterial);

        bottomY += middleWallHeight;
        CreateOrUpdateVisibleWallLayer(
            UpperWallObjectName,
            bounds,
            innerRadius,
            safeSegments,
            bottomY,
            upperWallHeight,
            upperWallMaterial);
    }

    private void CreateOrUpdateVisibleWallLayer(
        string objectName,
        Bounds bounds,
        float innerRadius,
        int safeSegments,
        float bottomY,
        float height,
        Material layerMaterial)
    {
        GameObject wallObject = GetOrCreateGeneratedChild(objectName);
        wallObject.transform.SetParent(generatedRoot.transform, false);

        Mesh mesh = BuildRingMesh(bounds, innerRadius, wallThickness, height, safeSegments, bottomY);

        MeshFilter meshFilter = GetOrAddComponent<MeshFilter>(wallObject);
        MeshRenderer meshRenderer = GetOrAddComponent<MeshRenderer>(wallObject);
        meshRenderer.sharedMaterial = GetLayerWallMaterial(layerMaterial);

        MeshCollider meshCollider = GetOrAddComponent<MeshCollider>(wallObject);
        ReplaceSharedMesh(meshFilter, meshCollider, mesh);
    }

    private void CreateOrUpdateInvisibleTopCollider(Bounds bounds, float innerRadius, int safeSegments)
    {
        GameObject topObject = GetOrCreateGeneratedChild(InvisibleTopObjectName);
        SetLayerIfExists(topObject, InvisibleTopLayerName);

        Mesh mesh = BuildRingMesh(bounds, innerRadius, wallThickness, invisibleTopHeight, safeSegments, bounds.max.y + GetVisibleWallHeight());

        MeshCollider meshCollider = GetOrAddComponent<MeshCollider>(topObject);
        Mesh oldMesh = meshCollider.sharedMesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        DestroyGeneratedObject(oldMesh);
    }

    private T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
            return component;

        return target.AddComponent<T>();
    }

    private void ReplaceSharedMesh(MeshFilter meshFilter, MeshCollider meshCollider, Mesh mesh)
    {
        Mesh oldMesh = meshFilter.sharedMesh;
        meshFilter.sharedMesh = mesh;

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        DestroyGeneratedObject(oldMesh);
    }

    private void DestroyGeneratedChild(string childName)
    {
        if (generatedRoot == null)
            return;

        Transform child = generatedRoot.transform.Find(childName);
        if (child != null)
            DestroyGeneratedObject(child.gameObject);
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

    private Material GetLayerWallMaterial(Material layerMaterial)
    {
        return layerMaterial != null ? layerMaterial : GetWallMaterial();
    }

    private float GetVisibleWallHeight()
    {
        if (!useLayeredVisibleWall)
            return wallHeight;

        return lowerWallHeight + middleWallHeight + upperWallHeight;
    }

    private void ApplyWallMaterialsToGeneratedWalls()
    {
        if (generatedRoot == null)
            return;

        ApplyMaterialToGeneratedWall(VisibleWallObjectName, GetWallMaterial());
        ApplyMaterialToGeneratedWall(LowerWallObjectName, GetLayerWallMaterial(lowerWallMaterial));
        ApplyMaterialToGeneratedWall(MiddleWallObjectName, GetLayerWallMaterial(middleWallMaterial));
        ApplyMaterialToGeneratedWall(UpperWallObjectName, GetLayerWallMaterial(upperWallMaterial));

        lastWallMaterial = wallMaterial;
        lastLowerWallMaterial = lowerWallMaterial;
        lastMiddleWallMaterial = middleWallMaterial;
        lastUpperWallMaterial = upperWallMaterial;
    }

    private void ApplyMaterialToGeneratedWall(string objectName, Material material)
    {
        Transform wallTransform = generatedRoot.transform.Find(objectName);
        if (wallTransform == null)
            return;

        MeshRenderer meshRenderer = wallTransform.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            return;

        meshRenderer.sharedMaterial = material;
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
