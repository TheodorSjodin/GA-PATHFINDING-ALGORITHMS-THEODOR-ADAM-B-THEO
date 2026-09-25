using UnityEngine;

public class Grid
{
    public int width;
    public int height;

    private GridCell[,] cells;

    public Grid(int width, int height)
    {
        this.width = width;
        this.height = height;

        cells = new GridCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                cells[x, z] = new GridCell(x, z);
            }
        }
    }

    public GridCell GetCell(int x, int z)
    {
        if (x < 0 || x >= width || z < 0 || z >= height)
            return null;

        return cells[x, z];
    }
}