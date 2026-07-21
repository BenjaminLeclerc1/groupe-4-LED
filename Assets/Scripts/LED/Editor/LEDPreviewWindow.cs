#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LEDPreviewWindow : EditorWindow
{
    const string WindowTitle = "Mur LED LAPS";

    LEDWallSimulator _simulator;
    Texture2D _previewTexture;
    int _pixelScale = 4;
    Vector2 _scroll;
    bool _autoSetupAttempted;

    [MenuItem("LED/Ouvrir la preview")]
    public static void Open()
    {
        var window = GetWindow<LEDPreviewWindow>(false, WindowTitle, true);
        window.minSize = new Vector2(360, 480);
        window.Show();
    }

    void OnEnable()
    {
        titleContent = new GUIContent(WindowTitle);
        EditorApplication.update += OnEditorUpdate;
        TryAutoSetupScene();
        RefreshPreview();
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        DestroyPreviewTexture();
    }

    void TryAutoSetupScene()
    {
        if (_autoSetupAttempted || Application.isPlaying)
            return;

        _autoSetupAttempted = true;

        if (FindActiveSimulator() != null)
            return;

        LEDMatrixSetupMenu.SetupSceneFromPreview(openPreviewWindow: false);
    }

    void OnEditorUpdate()
    {
        var activeSimulator = FindActiveSimulator();
        if (activeSimulator == _simulator)
            return;

        _simulator = activeSimulator;
        RefreshPreview();
        Repaint();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Mur LED LAPS — 2m × 2m", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "128 × 128 LED visibles, 64 bandes serpentin, 128 univers ArtNet.\n" +
            "Clique « Configurer la scène » puis regarde la Scene view ou appuie sur Play.",
            MessageType.Info);

        _simulator = (LEDWallSimulator)EditorGUILayout.ObjectField("Simulateur", _simulator, typeof(LEDWallSimulator), true);
        _pixelScale = EditorGUILayout.IntSlider("Zoom", _pixelScale, 2, 8);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Actualiser"))
            RefreshPreview();

        if (GUILayout.Button("Configurer la scène"))
            LEDMatrixSetupMenu.SetupSceneFromPreview(openPreviewWindow: false);
        EditorGUILayout.EndHorizontal();

        DrawWallInfo();
        EditorGUILayout.Space(8);

        if (_previewTexture == null)
            RefreshPreview();

        if (_previewTexture == null)
        {
            EditorGUILayout.HelpBox("Aucune preview disponible.", MessageType.Warning);
            return;
        }

        var previewSize = LEDWallConfig.VisibleWidth * _pixelScale;
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        var rect = GUILayoutUtility.GetRect(previewSize + 20f, previewSize + 20f);
        var frame = new Rect(rect.x + 10f, rect.y + 10f, previewSize, previewSize);

        EditorGUI.DrawRect(new Rect(frame.x - 3f, frame.y - 3f, frame.width + 6f, frame.height + 6f), new Color(0.2f, 0.2f, 0.2f));
        EditorGUI.DrawRect(frame, Color.black);
        GUI.DrawTexture(frame, _previewTexture, ScaleMode.StretchToFill, false);
        EditorGUILayout.EndScrollView();
    }

    void DrawWallInfo()
    {
        EditorGUILayout.LabelField("Spécifications", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Taille physique : {LEDWallConfig.PhysicalSizeMeters} m × {LEDWallConfig.PhysicalSizeMeters} m");
        EditorGUILayout.LabelField($"Résolution : {LEDWallConfig.VisibleWidth} × {LEDWallConfig.VisibleHeight} LED");
        EditorGUILayout.LabelField($"Bandes : {LEDWallConfig.StripCount}  |  Univers : {LEDWallConfig.UniverseCount}");
        EditorGUILayout.LabelField($"Contrôleurs : {string.Join(", ", LEDWallConfig.ControllerIps)}");
    }

    void RefreshPreview()
    {
        DestroyPreviewTexture();
        _previewTexture = LEDEditorPreviewHelper.CaptureSimulatorPreview(_simulator);
    }

    void DestroyPreviewTexture()
    {
        if (_previewTexture == null)
            return;

        DestroyImmediate(_previewTexture);
        _previewTexture = null;
    }

    static LEDWallSimulator FindActiveSimulator()
    {
        if (Selection.activeGameObject != null)
        {
            var selected = Selection.activeGameObject.GetComponent<LEDWallSimulator>();
            if (selected != null)
                return selected;
        }

        return Object.FindAnyObjectByType<LEDWallSimulator>();
    }
}
#endif
