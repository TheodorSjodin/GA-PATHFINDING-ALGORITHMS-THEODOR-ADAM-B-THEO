using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class BeanBagPathfinding : MonoBehaviour
{
    public enum Algorithm
    {
        BFS,
        DFS,
        Dijkstra,
        AStar,
        GreedyBestFirst
    }

    [Header("Path Settings")]
    public float moveSpeed = 3f;

    [Tooltip("Which algorithm's path the BeanBag should follow.")]
    public Algorithm animateAlgorithm = Algorithm.BFS;

    [Header("Results Logging")]
    public bool saveResults = true;

    [Header("Benchmark")]
    public bool runOnStart = true;

    private GridSettings settings;
    private ObstacleManager obstacleManager;
    private List<Vector2Int> path;
    private int exploredNodes;

    // Minnesmått (antal poster, inte byte):
    // peakFrontier = största storleken på kön/stacken/heapen under sökningen
    // storedNodes  = totalt antal poster i alla hjälpstrukturer (cameFrom, gScore, visited)
    private int peakFrontier;
    private int storedNodes;

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private readonly Algorithm[] algorithms =
    {
        Algorithm.BFS,
        Algorithm.DFS,
        Algorithm.Dijkstra,
        Algorithm.AStar,
        Algorithm.GreedyBestFirst
    };

    void Start()
    {
        settings = FindAnyObjectByType<GridSettings>();
        obstacleManager = FindAnyObjectByType<ObstacleManager>();

        if (settings == null || obstacleManager == null)
        {
            UnityEngine.Debug.LogError(
                "GridSettings or ObstacleManager not found!"
            );
            return;
        }

        if (runOnStart)
        {
            RunAlgorithmsForCurrentGrid(true);
        }
    }

    public void RunAlgorithmsForCurrentGrid(bool animateSelectedPath = false)
    {
        settings = FindAnyObjectByType<GridSettings>();
        obstacleManager = FindAnyObjectByType<ObstacleManager>();

        if (settings == null || obstacleManager == null)
        {
            UnityEngine.Debug.LogError(
                "GridSettings or ObstacleManager not found!"
            );
            return;
        }

        path = null;

        UnityEngine.Debug.Log(
            "Starting pathfinding benchmark..."
        );

        // BFS ligger först i 'algorithms' och ger referenslängden (kortaste vägen)
        // som övriga algoritmer jämförs mot på samma karta.
        int bfsLength = -1;

        foreach (Algorithm algorithm in algorithms)
        {
            exploredNodes = 0;
            peakFrontier = 0;
            storedNodes = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Vector2Int> result = FindPath(
                algorithm,
                settings.StartPosition,
                settings.GoalPosition
            );

            stopwatch.Stop();

            double searchTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            bool found = result != null;
            int length = found ? result.Count - 1 : 0;

            bool validPath = ValidatePath(
                result,
                settings.StartPosition,
                settings.GoalPosition
            );

            if (algorithm == Algorithm.BFS && found)
            {
                bfsLength = length;
            }

            // Sökvägsöverskott = väglängd / BFS-längd (1.000 = lika kort som kortaste vägen).
            // NaN om BFS inte hittade någon väg eller om start = mål.
            double pathOverhead =
                (found && bfsLength > 0)
                    ? (double)length / bfsLength
                    : double.NaN;

            UnityEngine.Debug.Log(
                algorithm +
                " | Time: " +
                searchTimeMs.ToString("F3", CultureInfo.InvariantCulture) +
                " ms | Path length: " + length +
                " | Explored nodes: " + exploredNodes +
                " | Overhead: " +
                (double.IsNaN(pathOverhead)
                    ? "n/a"
                    : pathOverhead.ToString(
                        "F3", CultureInfo.InvariantCulture)) +
                " | Peak frontier: " + peakFrontier +
                " | Stored nodes: " + storedNodes +
                " | Found: " + found +
                " | Valid: " + validPath
            );

            if (saveResults)
            {
                SaveResult(
                    algorithm,
                    searchTimeMs,
                    found,
                    length,
                    pathOverhead,
                    peakFrontier,
                    storedNodes
                );
            }

            if (algorithm == animateAlgorithm)
            {
                path = result;
            }
        }

        UnityEngine.Debug.Log("All algorithms finished.");

        if (animateSelectedPath && path != null)
        {
            StartCoroutine(FollowPath());
        }
    }

    void SaveResult(
        Algorithm algorithm,
        double searchTimeMs,
        bool pathFound,
        int pathLength,
        double pathOverhead,
        int peakFrontierSize,
        int storedNodeCount)
    {
        try
        {
            string filePath = Path.Combine(
                Application.persistentDataPath,
                "PathfindingResults.csv"
            );

            bool fileExists = File.Exists(filePath);

            using (StreamWriter writer = new StreamWriter(
                filePath, true, new UTF8Encoding(true)))
            {
                if (!fileExists ||
                    new FileInfo(filePath).Length == 0)
                {
                    writer.WriteLine(
                        "Timestamp,Algorithm,Width,Height," +
                        "ObstaclePercentage,SearchTimeMs," +
                        "PathLength,PathFound," +
                        "PathOverhead,PeakFrontier,StoredNodes"
                    );
                }

                string timestamp = DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture
                );

                string row = string.Join(",",
                    timestamp,
                    algorithm.ToString(),
                    settings.width.ToString(
                        CultureInfo.InvariantCulture),
                    settings.height.ToString(
                        CultureInfo.InvariantCulture),
                    (obstacleManager.ObstaclePercentage * 100f)
                        .ToString("F2", CultureInfo.InvariantCulture),
                    searchTimeMs.ToString(
                        "F3", CultureInfo.InvariantCulture),
                    pathLength.ToString(
                        CultureInfo.InvariantCulture),
                    pathFound ? "Yes" : "No",
                    double.IsNaN(pathOverhead)
                        ? ""
                        : pathOverhead.ToString(
                            "F3", CultureInfo.InvariantCulture),
                    peakFrontierSize.ToString(
                        CultureInfo.InvariantCulture),
                    storedNodeCount.ToString(
                        CultureInfo.InvariantCulture)
                );

                writer.WriteLine(row);
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(
                "Could not save result: " + e.Message
            );
        }
    }

    List<Vector2Int> FindPath(
        Algorithm algorithm,
        Vector2Int start,
        Vector2Int goal)
    {
        switch (algorithm)
        {
            case Algorithm.BFS:
                return FindBFS(start, goal);

            case Algorithm.DFS:
                return FindDFS(start, goal);

            case Algorithm.Dijkstra:
                return FindBestFirst(start, goal, Algorithm.Dijkstra);

            case Algorithm.AStar:
                return FindBestFirst(start, goal, Algorithm.AStar);

            case Algorithm.GreedyBestFirst:
                return FindBestFirst(
                    start, goal, Algorithm.GreedyBestFirst);

            default:
                return null;
        }
    }

    List<Vector2Int> FindBFS(
        Vector2Int start, Vector2Int goal)
    {
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> cameFrom =
            new Dictionary<Vector2Int, Vector2Int>();

        frontier.Enqueue(start);
        cameFrom[start] = start;
        peakFrontier = 1;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            exploredNodes++;

            if (current == goal)
            {
                storedNodes = cameFrom.Count;
                return BuildPath(cameFrom, start, goal);
            }

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbour = current + direction;

                if (!IsInsideGrid(neighbour) ||
                    obstacleManager.IsBlocked(neighbour) ||
                    cameFrom.ContainsKey(neighbour))
                    continue;

                cameFrom[neighbour] = current;
                frontier.Enqueue(neighbour);

                if (frontier.Count > peakFrontier)
                    peakFrontier = frontier.Count;
            }
        }

        storedNodes = cameFrom.Count;
        return null;
    }

    List<Vector2Int> FindDFS(
        Vector2Int start, Vector2Int goal)
    {
        Stack<Vector2Int> frontier = new Stack<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> cameFrom =
            new Dictionary<Vector2Int, Vector2Int>();

        frontier.Push(start);
        cameFrom[start] = start;
        peakFrontier = 1;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Pop();
            exploredNodes++;

            if (current == goal)
            {
                storedNodes = cameFrom.Count;
                return BuildPath(cameFrom, start, goal);
            }

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbour = current + direction;

                if (!IsInsideGrid(neighbour) ||
                    obstacleManager.IsBlocked(neighbour) ||
                    cameFrom.ContainsKey(neighbour))
                    continue;

                cameFrom[neighbour] = current;
                frontier.Push(neighbour);

                if (frontier.Count > peakFrontier)
                    peakFrontier = frontier.Count;
            }
        }

        storedNodes = cameFrom.Count;
        return null;
    }

    List<Vector2Int> FindBestFirst(
        Vector2Int start,
        Vector2Int goal,
        Algorithm algorithm)
    {
        MinHeap frontier = new MinHeap();

        Dictionary<Vector2Int, Vector2Int> cameFrom =
            new Dictionary<Vector2Int, Vector2Int>();

        Dictionary<Vector2Int, int> gScore =
            new Dictionary<Vector2Int, int>();

        HashSet<Vector2Int> visited =
            new HashSet<Vector2Int>();

        cameFrom[start] = start;
        gScore[start] = 0;

        frontier.Enqueue(start, GetPriority(start, goal, 0, algorithm), 0);
        peakFrontier = 1;

        while (frontier.Count > 0)
        {
            HeapEntry entry = frontier.Dequeue();
            Vector2Int current = entry.position;

            if (algorithm != Algorithm.GreedyBestFirst)
            {
                if (entry.cost > gScore[current])
                    continue;
            }
            else
            {
                if (!visited.Add(current))
                    continue;
            }

            exploredNodes++;

            if (current == goal)
            {
                storedNodes = cameFrom.Count + gScore.Count + visited.Count;
                return BuildPath(cameFrom, start, goal);
            }

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbour = current + direction;

                if (!IsInsideGrid(neighbour) ||
                    obstacleManager.IsBlocked(neighbour))
                    continue;

                int tentativeG = gScore[current] + 1;

                if (algorithm == Algorithm.GreedyBestFirst)
                {
                    if (cameFrom.ContainsKey(neighbour))
                        continue;

                    cameFrom[neighbour] = current;
                    gScore[neighbour] = tentativeG;

                    frontier.Enqueue(
                        neighbour,
                        GetPriority(
                            neighbour, goal, tentativeG, algorithm),
                        tentativeG
                    );

                    if (frontier.Count > peakFrontier)
                        peakFrontier = frontier.Count;
                }
                else
                {
                    if (gScore.ContainsKey(neighbour) &&
                        tentativeG >= gScore[neighbour])
                        continue;

                    cameFrom[neighbour] = current;
                    gScore[neighbour] = tentativeG;

                    frontier.Enqueue(
                        neighbour,
                        GetPriority(
                            neighbour, goal, tentativeG, algorithm),
                        tentativeG
                    );

                    if (frontier.Count > peakFrontier)
                        peakFrontier = frontier.Count;
                }
            }
        }

        storedNodes = cameFrom.Count + gScore.Count + visited.Count;
        return null;
    }

    int GetPriority(
        Vector2Int position,
        Vector2Int goal,
        int gCost,
        Algorithm algorithm)
    {
        int heuristic =
            Mathf.Abs(position.x - goal.x) +
            Mathf.Abs(position.y - goal.y);

        switch (algorithm)
        {
            case Algorithm.Dijkstra:
                return gCost;

            case Algorithm.AStar:
                return gCost + heuristic;

            case Algorithm.GreedyBestFirst:
                return heuristic;

            default:
                return 0;
        }
    }

    bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < settings.width &&
               position.y >= 0 &&
               position.y < settings.height;
    }

    bool ValidatePath(
        List<Vector2Int> result,
        Vector2Int start,
        Vector2Int goal)
    {
        if (result == null || result.Count == 0)
            return false;

        if (result[0] != start || result[result.Count - 1] != goal)
            return false;

        for (int i = 0; i < result.Count; i++)
        {
            Vector2Int cell = result[i];

            if (!IsInsideGrid(cell))
                return false;

            if (obstacleManager.IsBlocked(cell))
                return false;

            if (i > 0)
            {
                Vector2Int previous = result[i - 1];

                int distance =
                    Mathf.Abs(cell.x - previous.x) +
                    Mathf.Abs(cell.y - previous.y);

                // Each step must move exactly one cell.
                if (distance != 1)
                    return false;
            }
        }

        return true;
    }

    List<Vector2Int> BuildPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int start,
        Vector2Int goal)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int current = goal;

        while (current != start)
        {
            result.Add(current);
            current = cameFrom[current];
        }

        result.Add(start);
        result.Reverse();

        return result;
    }

    IEnumerator FollowPath()
    {
        foreach (Vector2Int cell in path)
        {
            Vector3 targetPosition = new Vector3(
                cell.x * settings.cellSize,
                transform.position.y,
                cell.y * settings.cellSize
            );

            while (Vector3.Distance(
                transform.position, targetPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPosition;
        }
    }

    struct HeapEntry
    {
        public Vector2Int position;
        public int priority;
        public int cost;

        public HeapEntry(
            Vector2Int position, int priority, int cost)
        {
            this.position = position;
            this.priority = priority;
            this.cost = cost;
        }
    }

    class MinHeap
    {
        private List<HeapEntry> items = new List<HeapEntry>();

        public int Count => items.Count;

        public void Enqueue(
            Vector2Int position, int priority, int cost)
        {
            HeapEntry entry = new HeapEntry(
                position, priority, cost);

            items.Add(entry);
            int index = items.Count - 1;

            while (index > 0)
            {
                int parent = (index - 1) / 2;

                if (items[parent].priority <=
                    items[index].priority)
                    break;

                Swap(parent, index);
                index = parent;
            }
        }

        public HeapEntry Dequeue()
        {
            HeapEntry result = items[0];
            int lastIndex = items.Count - 1;

            items[0] = items[lastIndex];
            items.RemoveAt(lastIndex);

            int index = 0;

            while (true)
            {
                int left = index * 2 + 1;
                int right = index * 2 + 2;
                int smallest = index;

                if (left < items.Count &&
                    items[left].priority <
                    items[smallest].priority)
                    smallest = left;

                if (right < items.Count &&
                    items[right].priority <
                    items[smallest].priority)
                    smallest = right;

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }

            return result;
        }

        private void Swap(int a, int b)
        {
            HeapEntry temp = items[a];
            items[a] = items[b];
            items[b] = temp;
        }
    }
}