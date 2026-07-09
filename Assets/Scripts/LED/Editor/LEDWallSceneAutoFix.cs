#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class LEDWallSceneAutoFix
{
    static LEDWallSceneAutoFix()
    {
        EditorApplication.delayCall += EnsureGameComponent;
        EditorApplication.playModeStateChanged += _ => EnsureGameComponent();
    }

    static void EnsureGameComponent()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var wall = Object.FindAnyObjectByType<LEDWallSimulator>();
        if (wall == null)
            return;

        if (wall.GetComponent<SkiDescentGame>() != null)
            return;

        Undo.AddComponent<SkiDescentGame>(wall.gameObject);
        EditorSceneManager.MarkSceneDirty(wall.gameObject.scene);
    }
}
#endif
