using UnityEngine;

/// <summary>
/// Indexation linéaire du buffer pixels (colonne/ligne → flat index).
/// Le câblage physique Art-Net est dans LedShow.Routing.LedWallLayout.
/// </summary>
public static class LEDWallLayout
{
    public static int GridToBufferIndex(int column, int row)
    {
        return row * LEDWallConfig.VisibleWidth + column;
    }

    public static Vector2Int BufferIndexToGrid(int index)
    {
        var column = index % LEDWallConfig.VisibleWidth;
        var row = index / LEDWallConfig.VisibleWidth;
        return new Vector2Int(column, row);
    }
}
