
using System.Collections;
using UnityEngine;

public class PathfindingBenchmark : MonoBehaviour
{
    [Header("Grid Sizes")]
    public Vector2Int[] gridSizes =
    {
        new Vector2Int(100, 100),
        new Vector2Int(250, 250),
        new Vector2Int(500, 500)
    };

    [Header("Obstacle Percentages")]
    [Range(0f, 1f)]
    public float[] obstaclePercentages =
    {
        0.10f, 0.20f, 0.30f, 0.40f
    };

    [Header("Benchmark Settings")]
    [Min(1)]
    public int repetitions = 5;

    [Header("References")]
    public GridSettings gridSettings;
    public GridRenderer gridRenderer;
    public ObstacleManager obstacleManager;
    public BeanBagPathfinding beanBagPathfinding;

    private bool benchmarkRunning;

    void Start()
    {
        if (gridSettings == null)
            gridSettings = FindFirstObjectByType<GridSettings>();

        if (gridRenderer == null)
            gridRenderer = FindFirstObjectByType<GridRenderer>();

        if (obstacleManager == null)
            obstacleManager = FindFirstObjectByType<ObstacleManager>();

        if (beanBagPathfinding == null)
            beanBagPathfinding =
                FindFirstObjectByType<BeanBagPathfinding>();

        if (gridSettings == null ||
            gridRenderer == null ||
            obstacleManager == null ||
            beanBagPathfinding == null)
        {
            Debug.LogError(
                "Benchmark failed: one or more references are missing."
            );
            return;
        }

        if (gridSizes == null || gridSizes.Length == 0 ||
            obstaclePercentages == null ||
            obstaclePercentages.Length == 0)
        {
            Debug.LogError(
                "Add at least one grid size and obstacle percentage."
            );
            return;
        }

        if (repetitions < 1)
        {
            Debug.LogError("Repetitions must be at least 1.");
            return;
        }

        benchmarkRunning = true;
        StartCoroutine(RunBenchmark());
    }

    IEnumerator RunBenchmark()
    {
        int totalConfigurations =
            gridSizes.Length *
            obstaclePercentages.Length *
            repetitions;

        int completedConfigurations = 0;

        Debug.Log(
            "BENCHMARK STARTED. Configurations: " +
            totalConfigurations +
            ". Expected algorithm results: " +
            (totalConfigurations * 5)
        );

        foreach (Vector2Int size in gridSizes)
        {
            foreach (float obstaclePercentage in obstaclePercentages)
            {
                for (int repetition = 1;
                     repetition <= repetitions;
                     repetition++)
                {
                    Debug.Log(
                        "Preparing test: " +
                        size.x + "x" + size.y +
                        " | Obstacles: " +
                        (obstaclePercentage * 100f) + "%" +
                        " | Repetition: " +
                        repetition + "/" + repetitions
                    );

                    // Configure the grid and choose a new goal.
                    gridSettings.ConfigureGrid(size.x, size.y);

                    // Set the obstacle percentage for this environment.
                    obstacleManager.obstaclePercentage =
                        Mathf.Clamp01(obstaclePercentage);

                    // Rebuild the environment.
                    obstacleManager.RegenerateEnvironment();
                    gridRenderer.RegenerateTerrain();

                    // Reset the BeanBag to the start cell.
                    Vector3 currentPosition =
                        beanBagPathfinding.transform.position;

                    Vector2Int start = gridSettings.StartPosition;

                    beanBagPathfinding.transform.position =
                        new Vector3(
                            start.x * gridSettings.cellSize,
                            currentPosition.y,
                            start.y * gridSettings.cellSize
                        );

                    // Allow Unity to finish processing the rebuild.
                    yield return null;

                    // Run all five algorithms on the same environment.
                    beanBagPathfinding.RunAlgorithmsForCurrentGrid(false);

                    completedConfigurations++;

                    Debug.Log(
                        "Completed configuration " +
                        completedConfigurations + "/" +
                        totalConfigurations
                    );

                    // Give Unity a frame before building the next map.
                    yield return null;
                }
            }
        }

        benchmarkRunning = false;

        Debug.Log(
            "BENCHMARK COMPLETE! Expected results: " +
            (totalConfigurations * 5) +
            ". Check PathfindingResults.csv."
        );
    }
}
