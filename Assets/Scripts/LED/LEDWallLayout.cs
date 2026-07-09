using UnityEngine;

/// <summary>
/// Mapping physique LAPS pour l'envoi ArtNet futur.
/// Seul GridToBufferIndex est utilisé par le rendu actuel.
/// </summary>
public static class LEDWallLayout
{
    public static int GetStripIndex(int column)
    {
        return column / 2;
    }

    public static bool IsDescendingColumn(int column)
    {
        return column % 2 == 1;
    }

    public static int GetQuarterIndex(int column)
    {
        return column / 32;
    }

    public static int GetControllerIndex(int column)
    {
        return Mathf.Clamp(GetQuarterIndex(column), 0, LEDWallConfig.ControllerIps.Length - 1);
    }

    /// <summary>
    /// Index 0-based sur la bande physique (259 LED dont certaines invisibles).
    /// </summary>
    public static int GridToStripLedIndex(int column, int row)
    {
        column = Mathf.Clamp(column, 0, LEDWallConfig.VisibleWidth - 1);
        row = Mathf.Clamp(row, 0, LEDWallConfig.VisibleHeight - 1);

        if (!IsDescendingColumn(column))
            return 1 + row;

        return 130 + (LEDWallConfig.VisibleHeight - 1 - row);
    }

    public static int GetUniverseForStrip(int stripIndex, int stripLedIndex)
    {
        stripIndex = Mathf.Clamp(stripIndex, 0, LEDWallConfig.StripCount - 1);
        var universeInController = stripIndex * LEDWallConfig.UniversesPerStrip;
        if (stripLedIndex >= LEDWallConfig.LedsPerUniverse)
            universeInController += 1;

        return universeInController;
    }

    public static int GetEntityBaseForColumn(int column)
    {
        var quarter = GetQuarterIndex(column);
        var columnInQuarter = column % 32;
        return 100 + quarter * 5000 + columnInQuarter * 300;
    }

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
