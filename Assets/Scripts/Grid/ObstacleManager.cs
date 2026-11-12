using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ObstacleManager : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Range(0f, 1f)]
    public float obstaclePercentage = 0.25f;

    public float ObstaclePercentage => obstaclePercentage;

    [Header("Visualization")]
    public bool renderObstacles = true;

    [Header("Large Obstacle Sizes")]
    [Range(1, 10)]
    public int minObstacleWidth = 2;

    [Range(1, 10)]
    public int maxObstacleWidth = 5;

    [Range(1, 10)]
    public int minObstacleHeight = 2;

    [Range(1, 10)]
    public int maxObstacleHeight = 5;

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

        RegenerateEnvironment();
    }

    public void RegenerateEnvironment()
    {
        if (settings == null)
        {
            settings = GetComponent<GridSettings>();
        }

        if (settings == null)
        {
            Debug.LogError("GridSettings component not found!");
            return;
        }

        // Remove the previous obstacle mesh, if present.
        Transform oldMesh = transform.Find("ObstacleMesh");

        if (oldMesh != null)
        {
            Destroy(oldMesh.gameObject);
        }

        int totalCells = settings.width * settings.height;

        blockedCells = new bool[totalCells];
        guaranteedPath = new bool[totalCells];

        GenerateGuaranteedPath();
        GenerateObstacles();

        if (renderObstacles)
        {
            CreateObstacleMesh();
        }
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
        int width = settings.width;
        int height = settings.height;
        int totalCells = width * height;

        int targetObstacleCells = Mathf.RoundToInt(
            totalCells * obstaclePercentage
        );

        int obstacleCount = 0;
        int maxAttempts = Mathf.Max(1000, totalCells / 2);
        int attempts = 0;

        while (obstacleCount < targetObstacleCells &&
               attempts < maxAttempts)
        {
            attempts++;

            int obstacleWidth = Random.Range(
                Mathf.Max(1, minObstacleWidth),
                Mathf.Max(
                    Mathf.Max(1, minObstacleWidth),
                    maxObstacleWidth
                ) + 1
            );

            int obstacleHeight = Random.Range(
                Mathf.Max(1, minObstacleHeight),
                Mathf.Max(
                    Mathf.Max(1, minObstacleHeight),
                    maxObstacleHeight
                ) + 1
            );

            if (allowRotation && Random.value < 0.5f)
            {
                int temp = obstacleWidth;
                obstacleWidth = obstacleHeight;
                obstacleHeight = temp;
            }

            if (obstacleWidth > width ||
                obstacleHeight > height)
            {
                continue;
            }

            int x = Random.Range(0, width - obstacleWidth + 1);
            int y = Random.Range(0, height - obstacleHeight + 1);

            int remainingCells = targetObstacleCells - obstacleCount;
            int shapeCells = obstacleWidth * obstacleHeight;

            if (shapeCells > remainingCells)
                continue;

            if (!CanPlaceRectangle(x, y, obstacleWidth, obstacleHeight))
                continue;

            for (int dy = 0; dy < obstacleHeight; dy++)
            {
                for (int dx = 0; dx < obstacleWidth; dx++)
                {
                    int index = (y + dy) * width + (x + dx);
                    blockedCells[index] = true;
                }
            }

            obstacleCount += shapeCells;
        }

        // Fill any remaining target cells with individual cells.
        // This helps approach the requested percentage if large
        // rectangles cannot fill the target exactly.
        if (obstacleCount < targetObstacleCells)
        {
            for (int i = 0;
                 i < totalCells && obstacleCount < targetObstacleCells;
                 i++)
            {
                if (blockedCells[i] || guaranteedPath[i])
                    continue;

                blockedCells[i] = true;
                obstacleCount++;
            }
        }

        float actualPercentage =
            (float)obstacleCount / totalCells * 100f;

        Debug.Log(
            "Generated " + obstacleCount +
            " obstacle cells out of " + totalCells +
            " (" + actualPercentage.ToString("F2") + "%)."
        );
    }

    bool CanPlaceRectangle(
        int x,
        int y,
        int rectangleWidth,
        int rectangleHeight)
    {
        for (int dy = 0; dy < rectangleHeight; dy++)
        {
            for (int dx = 0; dx < rectangleWidth; dx++)
            {
                int gridX = x + dx;
                int gridY = y + dy;

                if (gridX < 0 || gridX >= settings.width ||
                    gridY < 0 || gridY >= settings.height)
                {
                    return false;
                }

                int index = gridY * settings.width + gridX;

                if (blockedCells[index] || guaranteedPath[index])
                    return false;
            }
        }

        return true;
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