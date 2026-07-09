using UnityEngine;

public static class LEDWallBootstrap
{
    public static void EnsureWallExists()
    {
        if (Object.FindAnyObjectByType<LEDWallSimulator>() != null)
            return;

#if UNITY_EDITOR
        if (TryEditorSetup())
            return;
#endif

        LEDWallSceneFactory.CreateWall(LEDSpriteLoader.LoadSkieur());
    }

#if UNITY_EDITOR
    static bool TryEditorSetup()
    {
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        LEDMatrixSetupMenu.SetupSceneFromPreview(openPreviewWindow: false);
        return true;
    }

    [UnityEditor.InitializeOnLoad]
    static class EditorPlayHook
    {
        static EditorPlayHook()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state != UnityEditor.PlayModeStateChange.ExitingEditMode)
                return;

            EnsureWallExists();
        }
    }
#endif
}

public static class LEDWallPlayModeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureWallExists()
    {
        LEDWallBootstrap.EnsureWallExists();
    }
}
