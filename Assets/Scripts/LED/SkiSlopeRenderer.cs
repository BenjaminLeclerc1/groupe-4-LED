using UnityEngine;

public static class SkiSlopeRenderer
{
    public const int PlayerGroundTop = 34;

    static readonly Color SkyColor = new(0.45f, 0.68f, 0.95f);
    static readonly Color MountainColor = new(0.22f, 0.35f, 0.55f);
    static readonly Color SnowColor = new(0.92f, 0.96f, 1f);
    static readonly Color SnowShadowColor = new(0.78f, 0.86f, 0.96f);

    public static Color Sky => SkyColor;

    /// <summary>
    /// Pente fixe à l'écran : le sol sous le joueur ne bouge jamais.
    /// Seuls les motifs et les montagnes défilent avec le scroll.
    /// </summary>
    public static int GetScreenGroundTop(int screenColumn, int playerColumn)
    {
        var slope = (playerColumn - screenColumn) / 8;
        return Mathf.Max(14, PlayerGroundTop + slope);
    }

    public static void Draw(LEDWallBuffer buffer, float scroll, int playerColumn)
    {
        var width = LEDWallConfig.VisibleWidth;
        var height = LEDWallConfig.VisibleHeight;
        var scrollInt = Mathf.FloorToInt(scroll);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var groundTop = GetScreenGroundTop(x, playerColumn);

                if (y <= groundTop)
                {
                    var stripe = (x + scrollInt) % 14 < 2;
                    buffer.SetPixel(x, y, stripe ? SnowShadowColor : SnowColor);
                    continue;
                }

                if (y > 88 && IsMountainPixel(x, y, scrollInt))
                {
                    buffer.SetPixel(x, y, MountainColor);
                    continue;
                }

                buffer.SetPixel(x, y, SkyColor);
            }
        }
    }

    static bool IsMountainPixel(int screenX, int y, int scroll)
    {
        var worldX = screenX + scroll;
        var hill = Mathf.PerlinNoise(worldX * 0.04f, 0.2f);
        var mountainTop = 88 + Mathf.FloorToInt(hill * 18f);
        return y <= mountainTop;
    }
}
