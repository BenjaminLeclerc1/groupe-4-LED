using UnityEngine;

namespace LedShow.LED
{
    public static class SkiSlopeRenderer
    {
        public const int PlayerGroundTop = 34;

        static readonly Color SkyColor = new(0.03f, 0.06f, 0.14f);
        // Loin : bleu brumeux, peu contraste = 2e plan.
        static readonly Color FarMountainColor = new(0.16f, 0.24f, 0.40f);
        static readonly Color FarRidgeColor = new(0.38f, 0.48f, 0.62f);
        // Plus proche du fond : un peu plus marqué, neige sur les pics.
        static readonly Color NearMountainColor = new(0.22f, 0.30f, 0.46f);
        static readonly Color NearRidgeColor = new(0.78f, 0.86f, 0.98f);
        static readonly Color SnowColor = new(0.42f, 0.50f, 0.60f);
        static readonly Color SnowShadowColor = new(0.22f, 0.28f, 0.36f);

        // Parallaxe : les montagnes defilent plus lentement que la neige.
        const float FarParallax = 0.18f;
        const float NearParallax = 0.38f;

        public static Color Sky => SkyColor;

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
            var farScroll = Mathf.FloorToInt(scroll * FarParallax);
            var nearScroll = Mathf.FloorToInt(scroll * NearParallax);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var groundTop = GetScreenGroundTop(x, playerColumn);

                    if (y <= groundTop)
                    {
                        var stripe = (x + scrollInt) % 14 < 3;
                        buffer.SetPixel(x, y, stripe ? SnowShadowColor : SnowColor);
                        continue;
                    }

                    // D'abord la couche intermediaire (devant), puis la lointaine derriere.
                    var nearTop = GetNearPeakTop(x, nearScroll);
                    if (y >= 72 && y <= nearTop)
                    {
                        var distFromPeak = nearTop - y;
                        if (distFromPeak <= 1)
                            buffer.SetPixel(x, y, NearRidgeColor);
                        else if (distFromPeak <= 3 && IsSnowCapColumn(x, nearScroll))
                            buffer.SetPixel(x, y, Color.Lerp(NearMountainColor, NearRidgeColor, 0.55f));
                        else
                            buffer.SetPixel(x, y, NearMountainColor);
                        continue;
                    }

                    var farTop = GetFarPeakTop(x, farScroll);
                    if (y >= 92 && y <= farTop)
                    {
                        var isRidge = y >= farTop - 1;
                        buffer.SetPixel(x, y, isRidge ? FarRidgeColor : FarMountainColor);
                        continue;
                    }

                    buffer.SetPixel(x, y, SkyColor);
                }
            }
        }

        /// <summary>Pics triangulaires reguliers + variation = silhouette montagne.</summary>
        static int GetNearPeakTop(int screenX, int scroll)
        {
            var worldX = screenX + scroll;
            var primary = TrianglePeak(worldX, period: 28, amplitude: 18);
            var secondary = TrianglePeak(worldX + 11, period: 17, amplitude: 9);
            return 78 + primary + secondary / 2;
        }

        static int GetFarPeakTop(int screenX, int scroll)
        {
            var worldX = screenX + scroll;
            var primary = TrianglePeak(worldX, period: 36, amplitude: 12);
            var secondary = TrianglePeak(worldX + 19, period: 22, amplitude: 6);
            return 96 + primary + secondary / 2;
        }

        static int TrianglePeak(int worldX, int period, int amplitude)
        {
            var t = Mod(worldX, period);
            var half = period / 2;
            var dist = Mathf.Abs(t - half);
            // 0 au bord, amplitude au centre = sommet.
            return Mathf.RoundToInt((1f - dist / (float)half) * amplitude);
        }

        static bool IsSnowCapColumn(int screenX, int scroll)
        {
            // Neige seulement autour des sommets (centre des triangles).
            var worldX = screenX + scroll;
            var t = Mod(worldX, 28);
            var half = 14;
            return Mathf.Abs(t - half) <= 4;
        }

        static int Mod(int value, int modulus)
        {
            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
