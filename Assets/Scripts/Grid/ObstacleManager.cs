using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ObstacleManager : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Range(0f, 1f)]
    public float obstaclePercentage = 0.25f;

    public bool allow1x1 = true;
    public bool allow1x2 = true;
    public bool allow1x3 = true;
    public bool allow1x4 = true;
    public bool allow2x2 = true;

    public bool allowRotation = true;

    [Header("Guaranteed Path")]
    [Range(1, 20)]
    public int minStraightLength = 1;

    [Range(1, 20)]
    public int maxStraightLength = 6;

    private GridSettings settings;

    private bool[] blockedCells;

    private bool[] guaranteedPath;

    void Awake()
    {
        settings = GetComponent<GridSettings>();

        if (settings == null)
        {
            Debug.LogError("GridSettings component not found!");
            return;
        }

        int totalCells = settings.width * settings.height;

        blockedCells = new bool[totalCells];
        guaranteedPath = new bool[totalCells];

        GenerateGuaranteedPath();
        GenerateObstacles();
        CreateObstacleMesh();
    }

    void GenerateGuaranteedPath()
    {
        Vector2Int current =
            settings.StartPosition;

        Vector2Int goal =
            settings.GoalPosition;

        SetPath(current);

        bool moveHorizontal =
            Random.value < 0.5f;

        while (current != goal)
        {
            int straightLength =
                Random.Range(
                    minStraightLength,
                    maxStraightLength + 1
                );

            for (int i = 0;
                 i < straightLength &&
                 current != goal;
                 i++)
            {
                bool moved = false;

                if (moveHorizontal)
                {
                    if (current.x < goal.x)
                    {
                        current.x++;
                        moved = true;
                    }
                }
                else
                {
                    if (current.y < goal.y)
                    {
                        current.y++;
                        moved = true;
                    }
                }

                if (!moved)
                {
                    if (current.x < goal.x)
                        current.x++;
                    else if (current.y < goal.y)
                        current.y++;
                }

                SetPath(current);
            }

            moveHorizontal =
                !moveHorizontal;
        }

        Debug.Log(
            "Guaranteed winding path generated: " +
            CountPathCells() +
            " cells."
        );
    }

    void SetPath(Vector2Int position)
    {
        guaranteedPath[GetIndex(position)] = true;
    }

    void GenerateObstacles()
    {
        int totalCells =
            settings.width * settings.height;

        int targetObstacleCells =
            Mathf.RoundToInt(
                totalCells *
                obstaclePercentage
            );

        List<int> candidates = new List<int>();

        for (int i = 0; i < totalCells; i++)
        {
            if (!guaranteedPath[i])
                candidates.Add(i);
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temporary = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temporary;
        }

        int obstacleCount = 0;
        int attempts = 0;

        while (obstacleCount < targetObstacleCells &&
               attempts < candidates.Count * 2)
        {
            attempts++;

            Vector2Int[] shape =
                GetRandomShape();

            if (shape == null)
                continue;

            if (allowRotation &&
                Random.value > 0.5f)
            {
                shape = RotateShape(shape);
            }

            int remainingCells =
                targetObstacleCells -
                obstacleCount;

            if (shape.Length > remainingCells)
                continue;

            int candidateIndex = Random.Range(0, candidates.Count);
            int index = candidates[candidateIndex];

            Vector2Int position = new Vector2Int(
                index % settings.width,
                index / settings.width
            );

            if (!CanPlaceShape(
                shape,
                position))
            {
                continue;
            }

            foreach (Vector2Int cell in shape)
            {
                blockedCells[GetIndex(position + cell)] = true;
                obstacleCount++;
            }
        }

        for (int i = 0; i < candidates.Count &&
                        obstacleCount < targetObstacleCells; i++)
        {
            if (blockedCells[candidates[i]])
                continue;

            blockedCells[candidates[i]] = true;
            obstacleCount++;
        }

        float actualPercentage =
            (float)obstacleCount /
            totalCells *
            100f;

        Debug.Log(
            "Generated " +
            obstacleCount +
            " obstacle cells out of " +
            totalCells +
            " (" +
            actualPercentage.ToString("F1") +
            "%)."
        );
    }

    bool CanPlaceShape(
        Vector2Int[] shape,
        Vector2Int position)
    {
        foreach (Vector2Int cell in shape)
        {
            Vector2Int gridPosition =
                position + cell;

            if (!IsInsideGrid(gridPosition))
                return false;

            if (gridPosition ==
                settings.StartPosition)
                return false;

            if (gridPosition ==
                settings.GoalPosition)
                return false;

            int index = GetIndex(gridPosition);

            if (guaranteedPath[index])
                return false;

            if (blockedCells[index])
                return false;
        }

        return true;
    }

    Vector2Int[] GetRandomShape()
    {
        List<Vector2Int[]> shapes =
            new List<Vector2Int[]>();

        if (allow1x1)
        {
            shapes.Add(
                new Vector2Int[]
                {
                    new Vector2Int(0, 0)
                }
            );
        }

        if (allow1x2)
        {
            shapes.Add(
                new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1)
                }
            );
        }

        if (allow1x3)
        {
            shapes.Add(
                new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, 2)
                }
            );
        }

        if (allow1x4)
        {
            shapes.Add(
                new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, 2),
                    new Vector2Int(0, 3)
                }
            );
        }

        if (allow2x2)
        {
            shapes.Add(
                new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                }
            );
        }

        if (shapes.Count == 0)
            return null;

        return shapes[
            Random.Range(
                0,
                shapes.Count
            )
        ];
    }

    Vector2Int[] RotateShape(
        Vector2Int[] shape)
    {
        Vector2Int[] rotated =
            new Vector2Int[
                shape.Length
            ];

        for (int i = 0;
             i < shape.Length;
             i++)
        {
            rotated[i] =
                new Vector2Int(
                    -shape[i].y,
                    shape[i].x
                );
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;

        foreach (Vector2Int cell in rotated)
        {
            if (cell.x < minX)
                minX = cell.x;

            if (cell.y < minY)
                minY = cell.y;
        }

        for (int i = 0;
             i < rotated.Length;
             i++)
        {
            rotated[i] -=
                new Vector2Int(
                    minX,
                    minY
                );
        }

        return rotated;
    }

    // --------------------------------------------------
    // ONE MESH FOR ALL OBSTACLES
    // --------------------------------------------------

    void CreateObstacleMesh()
    {
        GameObject obstacleRoot =
            new GameObject("ObstacleMesh");

        obstacleRoot.transform.SetParent(transform);
        obstacleRoot.transform.localPosition = Vector3.zero;

        Material material =
            new Material(
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                )
            );

        material.color =
            new Color(
                0.15f,
                0.15f,
                0.15f
            );

        List<Vector3> vertices =
            new List<Vector3>();

        List<int> triangles =
            new List<int>();

        float size =
            settings.cellSize * 0.9f;

        float height = 1f;
        int chunkIndex = 0;

        const int maxVerticesPerChunk = 100000;

        for (int y = 0; y < settings.height; y++)
        {
            for (int x = 0; x < settings.width; x++)
            {
                int index = y * settings.width + x;

                if (!blockedCells[index])
                    continue;

                Vector2Int cell = new Vector2Int(x, y);

            Vector3 center =
                new Vector3(
                    cell.x * settings.cellSize,
                    height / 2f,
                    cell.y * settings.cellSize
                );

            AddCube(
                vertices,
                triangles,
                center,
                size,
                height
            );

                if (vertices.Count >= maxVerticesPerChunk)
                {
                    CreateObstacleMeshChunk(
                        obstacleRoot.transform,
                        vertices,
                        triangles,
                        material,
                        chunkIndex
                    );

                    chunkIndex++;
                    vertices.Clear();
                    triangles.Clear();
                }
            }
        }

        if (vertices.Count > 0)
        {
            CreateObstacleMeshChunk(
                obstacleRoot.transform,
                vertices,
                triangles,
                material,
                chunkIndex
            );
        }

        Debug.Log(
            "Obstacle mesh created with " +
            CountBlockedCells() +
            " cells in " +
            (chunkIndex + 1) +
            " mesh chunk(s)."
        );
    }

    void CreateObstacleMeshChunk(
        Transform parent,
        List<Vector3> vertices,
        List<int> triangles,
        Material material,
        int chunkIndex)
    {
        GameObject chunkObject =
            new GameObject("ObstacleMesh_" + chunkIndex);

        chunkObject.transform.SetParent(parent);
        chunkObject.transform.localPosition = Vector3.zero;

        Mesh mesh = new Mesh();
        mesh.name = "ObstacleMesh_" + chunkIndex;
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter meshFilter =
            chunkObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer =
            chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
    }

    void AddCube(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 center,
        float size,
        float height)
    {
        float half = size / 2f;

        float bottom = center.y - height / 2f;

        float top = center.y + height / 2f;

        float x = center.x;

        float z = center.z;

        int front = vertices.Count;

        vertices.Add(new Vector3(x - half, bottom, z - half));
        vertices.Add(new Vector3(x + half, bottom, z - half));
        vertices.Add(new Vector3(x + half, top, z - half));
        vertices.Add(new Vector3(x - half, top, z - half));

        triangles.Add(front + 0);
        triangles.Add(front + 2);
        triangles.Add(front + 1);
        triangles.Add(front + 0);
        triangles.Add(front + 3);
        triangles.Add(front + 2);

        int back = vertices.Count;

        vertices.Add(new Vector3(x + half, bottom, z + half));
        vertices.Add(new Vector3(x - half, bottom, z + half));
        vertices.Add(new Vector3(x - half, top, z + half));
        vertices.Add(new Vector3(x + half, top, z + half));

        triangles.Add(back + 0);
        triangles.Add(back + 2);
        triangles.Add(back + 1);
        triangles.Add(back + 0);
        triangles.Add(back + 3);
        triangles.Add(back + 2);

        int left = vertices.Count;

        vertices.Add(new Vector3(x - half, bottom, z + half));
        vertices.Add(new Vector3(x - half, bottom, z - half));
        vertices.Add(new Vector3(x - half, top, z - half));
        vertices.Add(new Vector3(x - half, top, z + half));

        triangles.Add(left + 0);
        triangles.Add(left + 2);
        triangles.Add(left + 1);
        triangles.Add(left + 0);
        triangles.Add(left + 3);
        triangles.Add(left + 2);

        int right = vertices.Count;

        vertices.Add(new Vector3(x + half, bottom, z - half));
        vertices.Add(new Vector3(x + half, bottom, z + half));
        vertices.Add(new Vector3(x + half, top, z + half));
        vertices.Add(new Vector3(x + half, top, z - half));

        triangles.Add(right + 0);
        triangles.Add(right + 2);
        triangles.Add(right + 1);
        triangles.Add(right + 0);
        triangles.Add(right + 3);
        triangles.Add(right + 2);

        int topFace = vertices.Count;

        vertices.Add(new Vector3(x - half, top, z - half));
        vertices.Add(new Vector3(x + half, top, z - half));
        vertices.Add(new Vector3(x + half, top, z + half));
        vertices.Add(new Vector3(x - half, top, z + half));

        triangles.Add(topFace + 0);
        triangles.Add(topFace + 2);
        triangles.Add(topFace + 1);
        triangles.Add(topFace + 0);
        triangles.Add(topFace + 3);
        triangles.Add(topFace + 2);

        int bottomFace = vertices.Count;

        vertices.Add(new Vector3(x - half, bottom, z + half));
        vertices.Add(new Vector3(x + half, bottom, z + half));
        vertices.Add(new Vector3(x + half, bottom, z - half));
        vertices.Add(new Vector3(x - half, bottom, z - half));

        triangles.Add(bottomFace + 0);
        triangles.Add(bottomFace + 2);
        triangles.Add(bottomFace + 1);
        triangles.Add(bottomFace + 0);
        triangles.Add(bottomFace + 3);
        triangles.Add(bottomFace + 2);
    }

    bool IsInsideGrid(Vector2Int position)
    {
        return
            position.x >= 0 &&
            position.x < settings.width &&
            position.y >= 0 &&
            position.y < settings.height;
    }

    public bool IsBlocked(Vector2Int position)
    {
        if (!IsInsideGrid(position))
            return true;

        return blockedCells[GetIndex(position)];
    }

    public bool IsGuaranteedPath(Vector2Int position)
    {
        if (!IsInsideGrid(position))
            return false;

        return guaranteedPath[GetIndex(position)];
    }

    int GetIndex(Vector2Int position)
    {
        return position.y * settings.width + position.x;
    }

    int CountBlockedCells()
    {
        int count = 0;

        for (int i = 0; i < blockedCells.Length; i++)
        {
            if (blockedCells[i])
                count++;
        }

        return count;
    }

    int CountPathCells()
    {
        int count = 0;

        for (int i = 0; i < guaranteedPath.Length; i++)
        {
            if (guaranteedPath[i])
                count++;
        }

        return count;
    }
}