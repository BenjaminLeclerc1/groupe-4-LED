using UnityEngine;

public static class LEDSpriteLoader
{
    public static Texture2D LoadSkieur() => Load(LEDSpritePaths.SkieurResource);

    public static Texture2D LoadObstacle() => Load(LEDSpritePaths.ObstacleResource);

    public static Texture2D LoadSapin() => Load(LEDSpritePaths.SapinResource);

    public static Texture2D LoadPiaf() => Load(LEDSpritePaths.PiafResource);

    public static Texture2D Load(string resourcePath)
    {
        var texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
            return texture;

        var sprite = Resources.Load<Sprite>(resourcePath);
        return sprite != null ? sprite.texture : null;
    }
}
