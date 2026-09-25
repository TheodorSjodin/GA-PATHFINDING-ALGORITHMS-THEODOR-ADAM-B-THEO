using UnityEngine;

public class BorderRenderer : MonoBehaviour
{
    private GridSettings settings;

    public float wallHeight = 1f;
    public float wallThickness = 0.2f;

    void Start()
    {
        settings = GetComponent<GridSettings>();

        if (settings == null)
        {
            Debug.LogError("GridSettings component not found!");
            return;
        }

        CreateBorder();
    }

    void CreateBorder()
    {
        float width =
            settings.width * settings.cellSize;

        float height =
            settings.height * settings.cellSize;

        float centerX =
            (settings.width - 1) * settings.cellSize / 2f;

        float centerZ =
            (settings.height - 1) * settings.cellSize / 2f;

        // Bottom
        CreateWall(
            "Border_Bottom",
            new Vector3(
                centerX,
                wallHeight / 2f,
                -settings.cellSize / 2f - wallThickness / 2f
            ),
            new Vector3(
                width + wallThickness * 2f,
                wallHeight,
                wallThickness
            )
        );

        // Top
        CreateWall(
            "Border_Top",
            new Vector3(
                centerX,
                wallHeight / 2f,
                (settings.height - 1) * settings.cellSize
                    + settings.cellSize / 2f
                    + wallThickness / 2f
            ),
            new Vector3(
                width + wallThickness * 2f,
                wallHeight,
                wallThickness
            )
        );

        // Left
        CreateWall(
            "Border_Left",
            new Vector3(
                -settings.cellSize / 2f - wallThickness / 2f,
                wallHeight / 2f,
                centerZ
            ),
            new Vector3(
                wallThickness,
                wallHeight,
                height
            )
        );

        // Right
        CreateWall(
            "Border_Right",
            new Vector3(
                (settings.width - 1) * settings.cellSize
                    + settings.cellSize / 2f
                    + wallThickness / 2f,
                wallHeight / 2f,
                centerZ
            ),
            new Vector3(
                wallThickness,
                wallHeight,
                height
            )
        );
    }

    void CreateWall(
        string wallName,
        Vector3 position,
        Vector3 scale)
    {
        GameObject wall =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        wall.name = wallName;

        wall.transform.position =
            position;

        wall.transform.localScale =
            scale;

        wall.transform.SetParent(
            transform
        );
    }
}