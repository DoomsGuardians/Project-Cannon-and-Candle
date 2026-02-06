using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script to create water plane meshes with configurable subdivisions.
/// </summary>
public class WaterPlaneGenerator : MonoBehaviour
{
    [Header("Plane Settings")]
    [SerializeField] private float width = 10f;
    [SerializeField] private float length = 10f;
    [SerializeField] private int subdivisionX = 32;
    [SerializeField] private int subdivisionZ = 32;

    [Header("Material")]
    [SerializeField] private Material waterMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public void GeneratePlane()
    {
        // Get or create components
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Create mesh
        Mesh mesh = new Mesh();
        mesh.name = "Water Plane";

        int vertCountX = subdivisionX + 1;
        int vertCountZ = subdivisionZ + 1;
        int vertCount = vertCountX * vertCountZ;
        int triCount = subdivisionX * subdivisionZ * 6;

        Vector3[] vertices = new Vector3[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        Vector4[] tangents = new Vector4[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[triCount];

        // Generate vertices
        for (int z = 0; z < vertCountZ; z++)
        {
            for (int x = 0; x < vertCountX; x++)
            {
                int index = z * vertCountX + x;
                float xPos = (x / (float)subdivisionX - 0.5f) * width;
                float zPos = (z / (float)subdivisionZ - 0.5f) * length;

                vertices[index] = new Vector3(xPos, 0, zPos);
                normals[index] = Vector3.up;
                tangents[index] = new Vector4(1, 0, 0, 1);
                uvs[index] = new Vector2(x / (float)subdivisionX, z / (float)subdivisionZ);
            }
        }

        // Generate triangles
        int triIndex = 0;
        for (int z = 0; z < subdivisionZ; z++)
        {
            for (int x = 0; x < subdivisionX; x++)
            {
                int bottomLeft = z * vertCountX + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + vertCountX;
                int topRight = topLeft + 1;

                // First triangle
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;

                // Second triangle
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomRight;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.tangents = tangents;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        // Apply material
        if (waterMaterial != null)
        {
            meshRenderer.sharedMaterial = waterMaterial;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Water Plane")]
    private void GenerateInEditor()
    {
        GeneratePlane();
    }

    [MenuItem("GameObject/3D Object/Water Plane", false, 10)]
    static void CreateWaterPlane(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Water Plane");
        var generator = go.AddComponent<WaterPlaneGenerator>();
        generator.GeneratePlane();

        // Register undo
        Undo.RegisterCreatedObjectUndo(go, "Create Water Plane");
        Selection.activeObject = go;

        // Parent to context object
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
    }
#endif
}
