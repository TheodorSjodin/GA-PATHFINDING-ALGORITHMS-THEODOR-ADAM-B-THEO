using UnityEngine;

public class GridSettings : MonoBehaviour
{
    [Header("Map Size")]
    public int width = 100;
    public int height = 100;

    [Header("Cell Size")]
    public float cellSize = 1f;

    private Vector2Int goalPosition;
    private bool goalPositionInitialized;

    public Vector2Int StartPosition
    {
        get { return new Vector2Int(0, 0); }
    }

    public Vector2Int GoalPosition
    {
        get
        {
            if (!goalPositionInitialized)
            {
                InitializeGoalPosition();
            }

            return goalPosition;
        }
    }

    void InitializeGoalPosition()
    {
        int minX = width > 2 ? 1 : 0;
        int maxXExclusive = width > 2 ? width - 1 : width;

        int minY = height > 2 ? height / 2 : 0;
        int maxYExclusive = height > 2 ? height - 1 : height;

        goalPosition = new Vector2Int(
            Random.Range(minX, maxXExclusive),
            Random.Range(minY, maxYExclusive)
        );

        if (goalPosition == StartPosition && width > 1 && height > 1)
        {
            goalPosition = new Vector2Int(
                Mathf.Min(1, width - 1),
                Mathf.Min(1, height - 1)
            );
        }

        goalPositionInitialized = true;
    }
}