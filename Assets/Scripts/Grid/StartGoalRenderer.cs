using UnityEngine;

public class StartGoalRenderer : MonoBehaviour
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

        CreateMarker(
            new Vector3(
                settings.StartPosition.x * settings.cellSize,
                0.15f,
                settings.StartPosition.y * settings.cellSize
            ),
            "START",
            Color.green
        );

        CreateMarker(
            new Vector3(
                settings.GoalPosition.x * settings.cellSize,
                0.15f,
                settings.GoalPosition.y * settings.cellSize
            ),
            "STOP",
            Color.red
        );
    }

    void CreateMarker(
        Vector3 position,
        string markerName,
        Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(
            PrimitiveType.Cube
        );

        marker.name = markerName;
        marker.transform.position = position;

        marker.transform.localScale = new Vector3(
            settings.cellSize * 0.8f,
            0.2f,
            settings.cellSize * 0.8f
        );

        Renderer renderer = marker.GetComponent<Renderer>();
        renderer.material.color = color;

        marker.transform.SetParent(transform);
    }
}