using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class BeanBagPathfinding : MonoBehaviour
{
    [Header("Path Settings")]
    public float moveSpeed = 3f;

    private GridSettings settings;
    private ObstacleManager obstacleManager;

    private List<Vector2Int> path;

    [System.Obsolete]
    void Start()
    {
        settings = FindFirstObjectByType<GridSettings>();
        obstacleManager = FindFirstObjectByType<ObstacleManager>();

        if (settings == null)
        {
            UnityEngine.Debug.LogError(
                "No GridSettings found!"
            );
            return;
        }

        if (obstacleManager == null)
        {
            UnityEngine.Debug.LogError(
                "No ObstacleManager found!"
            );
            return;
        }

        Stopwatch stopwatch =
            Stopwatch.StartNew();

        path = FindPath(
            settings.StartPosition,
            settings.GoalPosition
        );

        stopwatch.Stop();

        UnityEngine.Debug.Log(
            "Path calculation time: " +
            stopwatch.Elapsed.TotalMilliseconds +
            " ms"
        );

        if (path != null)
        {
            UnityEngine.Debug.Log(
                "Path found! Length: " +
                path.Count +
                " cells"
            );

            StartCoroutine(FollowPath());
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                "No path to the goal was found!"
            );
        }
    }

    List<Vector2Int> FindPath(
        Vector2Int start,
        Vector2Int goal)
    {
        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> cameFrom =
            new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        cameFrom[start] = start;

        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();

            if (current == goal)
            {
                return BuildPath(
                    cameFrom,
                    start,
                    goal
                );
            }

            foreach (Vector2Int neighbour
                in GetNeighbours(current))
            {
                if (obstacleManager.IsBlocked(neighbour))
                {
                    continue;
                }

                if (!cameFrom.ContainsKey(neighbour))
                {
                    queue.Enqueue(neighbour);
                    cameFrom[neighbour] = current;
                }
            }
        }

        return null;
    }

    List<Vector2Int> BuildPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int start,
        Vector2Int goal)
    {
        List<Vector2Int> result =
            new List<Vector2Int>();

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

    List<Vector2Int> GetNeighbours(
        Vector2Int position)
    {
        List<Vector2Int> neighbours =
            new List<Vector2Int>();

        Vector2Int[] directions =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        foreach (Vector2Int direction
            in directions)
        {
            Vector2Int neighbour =
                position + direction;

            if (neighbour.x >= 0 &&
                neighbour.x < settings.width &&
                neighbour.y >= 0 &&
                neighbour.y < settings.height)
            {
                neighbours.Add(neighbour);
            }
        }

        return neighbours;
    }

    IEnumerator FollowPath()
    {
        foreach (Vector2Int cell in path)
        {
            Vector3 targetPosition =
                new Vector3(
                    cell.x * settings.cellSize,
                    transform.position.y,
                    cell.y * settings.cellSize
                );

            while (Vector3.Distance(
                transform.position,
                targetPosition) > 0.05f)
            {
                transform.position =
                    Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        moveSpeed * Time.deltaTime
                    );

                yield return null;
            }

            transform.position = targetPosition;
        }
    }
}