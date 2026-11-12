
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GridRenderer : MonoBehaviour
{
    [Header("Visualization")]
    public bool renderTerrain = true;

    private GridSettings settings;
    private Material terrainMaterial;
    private Mesh terrainMesh;

    void Start()
    {
        RegenerateTerrain();
    }

    public void RegenerateTerrain()
    {
        settings = GetComponent<GridSettings>();

        if (settings == null)
        {
            Debug.LogError("GridSettings component not found!");
            return;
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (!renderTerrain)
        {
            meshRenderer.enabled = false;
            return;
        }

        meshRenderer.enabled = true;
        CreateTerrainMesh();
    }

    void CreateTerrainMesh()
    {
        float minX = -settings.cellSize / 2f;
        float minZ = -settings.cellSize / 2f;

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

        Mesh newMesh = new Mesh();
        newMesh.name = "TerrainMesh";
        newMesh.vertices = vertices;
        newMesh.triangles = triangles;
        newMesh.uv = uv;
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (terrainMesh != null)
        {
            Destroy(terrainMesh);
        }

        terrainMesh = newMesh;
        meshFilter.sharedMesh = terrainMesh;

        if (terrainMaterial == null)
        {
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Lit"
            );

            if (shader != null)
            {
                terrainMaterial = new Material(shader);
                terrainMaterial.color =
                    new Color(0.35f, 0.35f, 0.35f);
            }
        }

        if (terrainMaterial != null)
        {
            GetComponent<MeshRenderer>().sharedMaterial =
                terrainMaterial;
        }
    }

    void OnDestroy()
    {
        if (terrainMesh != null)
        {
            Destroy(terrainMesh);
        }

        if (terrainMaterial != null)
        {
            Destroy(terrainMaterial);
        }
    }
}
