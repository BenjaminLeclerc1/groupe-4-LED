using UnityEngine;

public static class LEDWallBootstrap
{
    public static void EnsureWallExists()
    {
        if (Object.FindAnyObjectByType<LEDWallSimulator>() != null)
            return;

        LEDWallSceneFactory.CreateWall(LEDSpriteLoader.LoadSkieur());
    }
}

public static class LEDWallPlayModeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureWallExists()
    {
        LEDWallBootstrap.EnsureWallExists();
    }
}
