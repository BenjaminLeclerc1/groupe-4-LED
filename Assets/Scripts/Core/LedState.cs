using UnityEngine;

namespace LedShow.Core
{
    // The single source of truth passed between authoring, routing and debugging tools.
    // Nothing downstream needs to know how these pixels map to physical controllers.
    public class LedState
    {
        public int Width { get; }
        public int Height { get; }
        public Color32[] Pixels { get; }

        public LedState(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color32[width * height];
        }

        public Color32 Get(int x, int y) => Pixels[y * Width + x];

        public void Set(int x, int y, Color32 color) => Pixels[y * Width + x] = color;

        public void Fill(Color32 color)
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                Pixels[i] = color;
            }
        }
    }
}
