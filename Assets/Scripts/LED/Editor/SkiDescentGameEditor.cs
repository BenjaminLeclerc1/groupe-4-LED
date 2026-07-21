#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkiDescentGame))]
public class SkiDescentGameEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();

        if (!EditorGUI.EndChangeCheck())
            return;

        var game = (SkiDescentGame)target;
        game.ForceRender();
        SkiLevelTimelineWindow.InvalidatePreviewRender();
        SceneView.RepaintAll();
    }
}
#endif
