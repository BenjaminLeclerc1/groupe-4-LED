using UnityEngine;

public class LEDWallBuffer
{
    readonly Color[] _pixels = new Color[LEDWallConfig.VisibleLedCount];
    Texture2D _texture;

    public Texture2D Texture => _texture;
    public Color[] Pixels => _pixels;

    public void EnsureTexture()
    {
        if (_texture != null)
            return;

        _texture = new Texture2D(LEDWallConfig.VisibleWidth, LEDWallConfig.VisibleHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    public void Clear(Color color)
    {
        ClearPixels(color);
        Apply();
    }

    public void ClearPixels(Color color)
    {
        for (var i = 0; i < _pixels.Length; i++)
            _pixels[i] = color;
    }

    public void SetPixel(int column, int row, Color color)
    {
        if (!IsInside(column, row))
            return;

        _pixels[LEDWallLayout.GridToBufferIndex(column, row)] = color;
    }

    public Color GetPixel(int column, int row)
    {
        if (!IsInside(column, row))
            return Color.clear;

        return _pixels[LEDWallLayout.GridToBufferIndex(column, row)];
    }

    public void DrawTextureCentered(Texture2D source)
    {
        if (source == null)
            return;

        if (!LEDTextureUtility.TryGetPixels(source, out var sourcePixels, out var sourceWidth, out var sourceHeight))
            return;

        var position = new Vector2Int(
            (LEDWallConfig.VisibleWidth - sourceWidth) / 2,
            (LEDWallConfig.VisibleHeight - sourceHeight) / 2);

        DrawTexture(sourcePixels, sourceWidth, sourceHeight, position);
    }

    public void DrawTexture(Color[] sourcePixels, int sourceWidth, int sourceHeight, Vector2Int position, bool applyAfter = true)
    {
        for (var y = 0; y < sourceHeight; y++)
        {
            for (var x = 0; x < sourceWidth; x++)
            {
                var color = sourcePixels[y * sourceWidth + x];
                if (LEDTextureUtility.IsBackgroundPixel(color))
                    continue;

                SetPixel(position.x + x, position.y + y, color);
            }
        }

        if (applyAfter)
            Apply();
    }

    public void Apply()
    {
        EnsureTexture();
        _texture.SetPixels(_pixels);
        _texture.Apply();
    }

    public void Destroy()
    {
        if (_texture == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(_texture);
        else
            Object.DestroyImmediate(_texture);

        _texture = null;
    }

    static bool IsInside(int column, int row)
    {
        return column >= 0
            && column < LEDWallConfig.VisibleWidth
            && row >= 0
            && row < LEDWallConfig.VisibleHeight;
    }
}
