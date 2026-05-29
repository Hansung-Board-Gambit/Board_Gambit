using UnityEngine;

[ExecuteAlways]
public class CircularBoardSurface : MonoBehaviour
{
    [Header("Shape")]
    public float radius = 5f;
    public int segments = 96;

    [Header("Update")]
    public bool rebuildInEditor = true;

    private Mesh generatedMesh;
    private float lastRadius;
    private int lastSegments;

    private void OnEnable()
    {
        RebuildIfNeeded(true);
    }

    private void Start()
    {
        RebuildIfNeeded(true);
    }

    private void Update()
    {
        if (Application.isPlaying || rebuildInEditor)
            RebuildIfNeeded(false);
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.1f, radius);
        segments = Mathf.Clamp(segments, 12, 256);
        RebuildIfNeeded(true);
    }

    [ContextMenu("Rebuild Circular Board")]
    public void Rebuild()
    {
        RebuildIfNeeded(true);
    }

    private void RebuildIfNeeded(bool force)
    {
        int safeSegments = Mathf.Clamp(segments, 12, 256);
        float safeRadius = Mathf.Max(0.1f, radius);

        if (!force && generatedMesh != null && Mathf.Approximately(lastRadius, safeRadius) && lastSegments == safeSegments)
            return;

        Mesh mesh = BuildCircleMesh(safeRadius, safeSegments);
        ApplyMesh(mesh);

        generatedMesh = mesh;
        lastRadius = safeRadius;
        lastSegments = safeSegments;
    }

    private Mesh BuildCircleMesh(float circleRadius, int circleSegments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Generated Circular Board";
        mesh.hideFlags = HideFlags.DontSave;

        Vector3[] vertices = new Vector3[circleSegments + 1];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[circleSegments * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / circleSegments;
            float x = Mathf.Cos(angle) * circleRadius;
            float z = Mathf.Sin(angle) * circleRadius;
            vertices[i + 1] = new Vector3(x, 0f, z);
            uvs[i + 1] = new Vector2((x / circleRadius + 1f) * 0.5f, (z / circleRadius + 1f) * 0.5f);
        }

        for (int i = 0; i < circleSegments; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i == circleSegments - 1 ? 1 : i + 2;
            triangles[triangleIndex + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ApplyMesh(Mesh mesh)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
            meshFilter.sharedMesh = mesh;

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }
}
