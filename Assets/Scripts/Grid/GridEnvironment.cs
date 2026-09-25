using UnityEngine;

public class GridEnvironment : MonoBehaviour
{
    private GridSettings settings;

    public float floorThickness = 0.2f;
    public float borderHeight = 0.5f;
    public float borderThickness = 0.2f;

    void Start()
    {
        settings = GetComponent<GridSettings>();

        if (settings == null)
        {
            Debug.LogError("GridSettings component not found!");
            return;
        }

        CreateFloor();
        CreateBorder();
    }

    void CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(
            PrimitiveType.Cube
        );

        floor.name = "Floor";

        floor.transform.position = new Vector3(
            (settings.width - 1) * settings.cellSize / 2f,
            -0.2f,
            (settings.height - 1) * settings.cellSize / 2f
        );

        floor.transform.localScale = new Vector3(
            settings.width * settings.cellSize,
            floorThickness,
            settings.height * settings.cellSize
        );

        floor.transform.SetParent(transform);
    }

    void CreateBorder()
    {
        float totalWidth = settings.width * settings.cellSize;
        float totalHeight = settings.height * settings.cellSize;

        CreateWall(
            "Border_Bottom",
            new Vector3(
                (settings.width - 1) * settings.cellSize / 2f,
                borderHeight / 2f,
                -settings.cellSize / 2f - borderThickness / 2f
            ),
            new Vector3(
                totalWidth + borderThickness * 2f,
                borderHeight,
                borderThickness
            )
        );

        CreateWall(
            "Border_Top",
            new Vector3(
                (settings.width - 1) * settings.cellSize / 2f,
                borderHeight / 2f,
                (settings.height - 1) * settings.cellSize
                    + settings.cellSize / 2f
                    + borderThickness / 2f
            ),
            new Vector3(
                totalWidth + borderThickness * 2f,
                borderHeight,
                borderThickness
            )
        );

        CreateWall(
            "Border_Left",
            new Vector3(
                -settings.cellSize / 2f - borderThickness / 2f,
                borderHeight / 2f,
                (settings.height - 1) * settings.cellSize / 2f
            ),
            new Vector3(
                borderThickness,
                borderHeight,
                totalHeight
            )
        );

        CreateWall(
            "Border_Right",
            new Vector3(
                (settings.width - 1) * settings.cellSize
                    + settings.cellSize / 2f
                    + borderThickness / 2f,
                borderHeight / 2f,
                (settings.height - 1) * settings.cellSize / 2f
            ),
            new Vector3(
                borderThickness,
                borderHeight,
                totalHeight
            )
        );
    }

    void CreateWall(
        string wallName,
        Vector3 position,
        Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(
            PrimitiveType.Cube
        );

        wall.name = wallName;
        wall.transform.position = position;
        wall.transform.localScale = scale;

        wall.transform.SetParent(transform);
    }
}