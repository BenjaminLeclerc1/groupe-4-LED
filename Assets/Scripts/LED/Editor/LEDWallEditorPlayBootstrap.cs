#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class LEDWallEditorPlayBootstrap
{
    static LEDWallEditorPlayBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        if (Object.FindAnyObjectByType<LEDWallSimulator>() != null)
            return;

        LEDMatrixSetupMenu.SetupSceneFromPreview(openPreviewWindow: false);
    }
}
#endif
