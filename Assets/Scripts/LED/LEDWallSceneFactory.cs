using UnityEngine;

public static class LEDWallSceneFactory
{
    public static LEDWallSimulator CreateWall(Texture2D sourceTexture)
    {
        var existing = Object.FindAnyObjectByType<LEDWallSimulator>();
        if (existing != null)
        {
            if (sourceTexture != null)
                existing.SetSourceTexture(sourceTexture);
            return existing;
        }

        return LEDWallSceneBuilder.EnsureWall(LEDWallBuildOptions.Runtime, sourceTexture).Simulator;
    }

    public static Texture2D LoadDefaultSkieur() => LEDSpriteLoader.LoadSkieur();

    public static Texture2D LoadDefaultObstacle() => LEDSpriteLoader.LoadObstacle();
}
