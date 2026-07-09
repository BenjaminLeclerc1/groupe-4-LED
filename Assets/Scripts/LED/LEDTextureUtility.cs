using UnityEngine;

public static class LEDTextureUtility
{
    public static bool TryGetPixels(Texture2D texture, out Color[] pixels, out int width, out int height, bool stripBlack = false)
    {
        pixels = null;
        width = 0;
        height = 0;

        if (texture == null)
            return false;

        width = texture.width;
        height = texture.height;

        if (texture.isReadable)
        {
            pixels = texture.GetPixels();
            StripBackgroundPixels(pixels, stripBlack);
            return pixels != null && pixels.Length == width * height;
        }

        if (!TryCopyViaRenderTexture(texture, out pixels, stripBlack))
            return false;

        return pixels != null;
    }

    static bool TryCopyViaRenderTexture(Texture2D texture, out Color[] pixels, bool stripBlack)
    {
        pixels = null;

        var renderTexture = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.ARGB32);

        var previous = RenderTexture.active;
        Graphics.Blit(texture, renderTexture);
        RenderTexture.active = renderTexture;

        var readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        pixels = readable.GetPixels();
        StripBackgroundPixels(pixels, stripBlack);
        Object.DestroyImmediate(readable);
        return pixels != null;
    }

    public static void StripBackgroundPixels(Color[] pixels, bool stripBlack = false)
    {
        if (pixels == null)
            return;

        for (var i = 0; i < pixels.Length; i++)
        {
            if (IsBackgroundPixel(pixels[i], stripBlack))
                pixels[i] = Color.clear;
        }
    }

    public static bool IsBackgroundPixel(Color color, bool stripBlack = false)
    {
        if (color.a <= 0.01f)
            return true;

        if (stripBlack && color.r <= 0.05f && color.g <= 0.05f && color.b <= 0.05f)
            return true;

        return color.r >= 0.94f && color.g >= 0.94f && color.b >= 0.94f;
    }

    public static void ApplyToRenderer(Renderer renderer, Texture texture)
    {
        if (renderer == null || texture == null)
            return;

        var material = renderer.sharedMaterial;
        if (material == null)
            return;

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        else
            material.mainTexture = texture;
    }
}
