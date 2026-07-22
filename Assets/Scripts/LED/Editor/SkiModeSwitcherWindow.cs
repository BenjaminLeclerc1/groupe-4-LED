#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using LedShow.LED;

/// <summary>
/// Petit switch Timeline Mode / Game Mode.
/// </summary>
public class SkiModeSwitcherWindow : EditorWindow
{
    enum SkiUiMode
    {
        Timeline,
        Game
    }

    const string WindowTitle = "Ski Modes";
    static SkiUiMode _mode = SkiUiMode.Timeline;

    [MenuItem("LED/Modes Ski (Timeline / Game)")]
    public static void Open()
    {
        var window = GetWindow<SkiModeSwitcherWindow>(false, WindowTitle, true);
        window.minSize = new Vector2(280, 160);
        window.Show();
    }

    void OnEnable()
    {
        titleContent = new GUIContent(WindowTitle);
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    void OnPlayModeChanged(PlayModeStateChange _)
    {
        Repaint();
    }

    void OnGUI()
    {
        var game = Object.FindAnyObjectByType<SkiDescentGame>();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawModeButton(
                "Timeline Mode",
                "Éditer / preview la timeline (sans Play Unity).",
                SkiUiMode.Timeline,
                new Color(0.25f, 0.45f, 0.75f));

            GUILayout.Space(8);

            DrawModeButton(
                "Game Mode",
                "Mini-jeu classique : obstacles générés tout seuls.",
                SkiUiMode.Game,
                new Color(0.25f, 0.65f, 0.35f));
        }

        EditorGUILayout.Space(12);
        DrawStatus(game);
    }

    void DrawModeButton(string label, string tooltip, SkiUiMode mode, Color tint)
    {
        var selected = _mode == mode;
        var rect = GUILayoutUtility.GetRect(120, 64, GUILayout.ExpandWidth(true));

        var bg = selected
            ? Color.Lerp(tint, Color.white, 0.25f)
            : new Color(tint.r * 0.55f, tint.g * 0.55f, tint.b * 0.55f, 1f);

        EditorGUI.DrawRect(rect, bg);

        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white },
            fontSize = 13
        };

        if (GUI.Button(rect, new GUIContent(label, tooltip), style))
        {
            if (mode == SkiUiMode.Timeline)
                EnterTimelineMode();
            else
                EnterGameMode();
        }

        if (selected)
        {
            Handles.BeginGUI();
            Handles.color = Color.white;
            Handles.DrawAAPolyLine(3f,
                new Vector3(rect.xMin + 1, rect.yMin + 1),
                new Vector3(rect.xMax - 1, rect.yMin + 1),
                new Vector3(rect.xMax - 1, rect.yMax - 1),
                new Vector3(rect.xMin + 1, rect.yMax - 1),
                new Vector3(rect.xMin + 1, rect.yMin + 1));
            Handles.EndGUI();
        }
    }

    void DrawStatus(SkiDescentGame game)
    {
        if (game == null)
        {
            EditorGUILayout.HelpBox(
                "Aucun SkiDescentGame dans la scène.\nLED → Configurer la scène d’abord.",
                MessageType.Warning);
            return;
        }

        var timelineName = game.LevelTimeline != null ? game.LevelTimeline.name : "(aucune)";
        var playLabel = EditorApplication.isPlaying ? "Play Unity ON" : "Play Unity OFF";
        var modeLabel = _mode == SkiUiMode.Timeline ? "Timeline" : "Game";

        EditorGUILayout.HelpBox(
            $"Mode actif : {modeLabel}\n" +
            $"{playLabel}\n" +
            $"Timeline assignée : {timelineName}\n" +
            $"useLevelTimeline : {game.UseLevelTimeline}",
            MessageType.Info);
    }

    static void EnterTimelineMode()
    {
        _mode = SkiUiMode.Timeline;

        var game = Object.FindAnyObjectByType<SkiDescentGame>();
        if (game != null)
        {
            if (!Application.isPlaying)
                Undo.RecordObject(game, "Timeline Mode");

            game.UseLevelTimeline = game.LevelTimeline != null;
            game.RecordObstaclesOnJump = false;

            if (!Application.isPlaying)
                EditorUtility.SetDirty(game);

            if (game.IsEditorPreviewActive)
                game.EndTimelinePreview();

            game.ShowHomeScreen();
        }

        SkiLevelTimelineWindow.Open();
        GetWindow<SkiModeSwitcherWindow>()?.Repaint();
    }

    static void EnterGameMode()
    {
        _mode = SkiUiMode.Game;

        var game = Object.FindAnyObjectByType<SkiDescentGame>();
        if (game == null)
        {
            EditorUtility.DisplayDialog(
                "Game Mode",
                "Aucun SkiDescentGame dans la scène.\nUtilise LED → Configurer la scène d’abord.",
                "OK");
            return;
        }

        if (!Application.isPlaying)
            Undo.RecordObject(game, "Game Mode");

        game.UseLevelTimeline = false;
        game.RecordObstaclesOnJump = false;

        if (!Application.isPlaying)
            EditorUtility.SetDirty(game);

        if (game.IsEditorPreviewActive)
            game.EndTimelinePreview();

        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;
        else
            game.ShowHomeScreen();

        GetWindow<SkiModeSwitcherWindow>()?.Repaint();
    }
}
#endif
