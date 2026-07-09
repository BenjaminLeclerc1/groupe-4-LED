#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LEDWallSimulator), true)]
public class LEDWallSimulatorEditor : Editor
{
    Texture2D _previewTexture;
    int _pixelScale = 4;

    void OnDisable()
    {
        DestroyPreviewTexture();
    }

    public override void OnInspectorGUI()
    {
        var simulator = (LEDWallSimulator)target;

        EditorGUI.BeginChangeCheck();
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
        var inspectorChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Preview mur LED", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{LEDWallConfig.PhysicalSizeMeters} m × {LEDWallConfig.PhysicalSizeMeters} m  |  {LEDWallConfig.VisibleWidth}×{LEDWallConfig.VisibleHeight} LED");
        _pixelScale = EditorGUILayout.IntSlider("Zoom inspecteur", _pixelScale, 2, 8);

        var refreshRequested = false;
        if (GUILayout.Button("Actualiser la preview"))
            refreshRequested = true;

        if (GUILayout.Button("Ouvrir la fenêtre Mur LED"))
            LEDPreviewWindow.Open();

        if (inspectorChanged || refreshRequested)
        {
            RefreshInspectorPreview(simulator);
        }
        else if (_previewTexture == null)
            RefreshInspectorPreview(simulator);

        if (_previewTexture == null)
            return;

        var size = LEDWallConfig.VisibleWidth * _pixelScale;
        var rect = GUILayoutUtility.GetRect(size + 8f, size + 8f);
        var frame = new Rect(rect.x + 4f, rect.y + 4f, size, size);

        EditorGUI.DrawRect(new Rect(frame.x - 1f, frame.y - 1f, frame.width + 2f, frame.height + 2f), Color.gray);
        EditorGUI.DrawRect(frame, Color.black);
        GUI.DrawTexture(frame, _previewTexture, ScaleMode.StretchToFill, false);
    }

    void RefreshInspectorPreview(LEDWallSimulator simulator)
    {
        DestroyPreviewTexture();
        _previewTexture = LEDEditorPreviewHelper.CaptureSimulatorPreview(simulator);
    }

    void DestroyPreviewTexture()
    {
        if (_previewTexture == null)
            return;

        DestroyImmediate(_previewTexture);
        _previewTexture = null;
    }
}
#endif
