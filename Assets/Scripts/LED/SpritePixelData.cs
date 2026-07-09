using UnityEngine;

public struct SpritePixelData
{
    public Color[] Pixels;
    public int Width;
    public int Height;
    public int HitMinX;
    public int HitMinY;
    public int HitMaxX;
    public int HitMaxY;

    public static SpritePixelData FromTexture(Texture2D texture, bool stripBlack = false)
    {
        var data = new SpritePixelData();
        if (texture == null || !LEDTextureUtility.TryGetPixels(texture, out var pixels, out var width, out var height, stripBlack))
            return data;

        data.Pixels = pixels;
        data.Width = width;
        data.Height = height;
        ComputeHitBounds(ref data);
        return data;
    }

    public bool HasPixels => Pixels != null && Pixels.Length > 0;

    static void ComputeHitBounds(ref SpritePixelData data)
    {
        data.HitMinX = data.Width;
        data.HitMinY = data.Height;
        data.HitMaxX = -1;
        data.HitMaxY = -1;

        for (var y = 0; y < data.Height; y++)
        {
            for (var x = 0; x < data.Width; x++)
            {
                if (LEDTextureUtility.IsBackgroundPixel(data.Pixels[y * data.Width + x]))
                    continue;

                if (x < data.HitMinX) data.HitMinX = x;
                if (y < data.HitMinY) data.HitMinY = y;
                if (x > data.HitMaxX) data.HitMaxX = x;
                if (y > data.HitMaxY) data.HitMaxY = y;
            }
        }

        if (data.HitMaxX < 0)
        {
            data.HitMinX = 0;
            data.HitMinY = 0;
            data.HitMaxX = data.Width - 1;
            data.HitMaxY = data.Height - 1;
        }
    }
}
