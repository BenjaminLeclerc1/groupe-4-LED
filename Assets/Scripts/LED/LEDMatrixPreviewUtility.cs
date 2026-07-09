using UnityEngine;

public static class LEDMatrixPreviewUtility
{
    public static Texture2D BuildMatrixTexture(Texture2D source, bool clearToBlack = true)
    {
        var buffer = new LEDWallBuffer();
        if (clearToBlack)
            buffer.Clear(Color.black);

        if (source != null)
            buffer.DrawTextureCentered(source);
        else
            buffer.Apply();

        buffer.EnsureTexture();
        var matrix = Object.Instantiate(buffer.Texture);
        matrix.hideFlags = HideFlags.HideAndDontSave;
        buffer.Destroy();
        return matrix;
    }
}
