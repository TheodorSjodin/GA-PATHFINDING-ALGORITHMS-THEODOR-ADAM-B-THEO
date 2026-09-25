using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GridRenderer : MonoBehaviour
{
    private GridSettings settings;

    void Start()
    {
        settings = GetComponent<GridSettings>();

        if (settings == null)
        {
            Debug.LogError("GridSettings component not found!");
            return;
        }

        CreateTerrainMesh();
    }

    void CreateTerrainMesh()
    {
        float minX =
            -settings.cellSize / 2f;

        float minZ =
            -settings.cellSize / 2f;

        float maxX =
            (settings.width - 1) * settings.cellSize
            + settings.cellSize / 2f;

        float maxZ =
            (settings.height - 1) * settings.cellSize
            + settings.cellSize / 2f;

        Vector3[] vertices =
        {
            new Vector3(minX, 0, minZ),
            new Vector3(maxX, 0, minZ),
            new Vector3(minX, 0, maxZ),
            new Vector3(maxX, 0, maxZ)
        };

        int[] triangles =
        {
            0, 2, 3,
            0, 3, 1
        };

        Vector2[] uv =
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        Mesh mesh = new Mesh();

        mesh.name = "TerrainMesh";

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter meshFilter =
            GetComponent<MeshFilter>();

        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer =
            GetComponent<MeshRenderer>();

        Material material =
            new Material(
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                )
            );

        material.color =
            new Color(
                0.35f,
                0.35f,
                0.35f
            );

        meshRenderer.material = material;
    }
}