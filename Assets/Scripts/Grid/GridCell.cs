using UnityEngine;

public class GridCell
{
    public int x;
    public int z;

    public bool walkable;

    public GridCell(int x, int z, bool walkable = true)
    {
        this.x = x;
        this.z = z;
        this.walkable = walkable;
    }
}