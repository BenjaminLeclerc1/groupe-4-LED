#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LedShow.LED;

public class SkiLevelTimelineWindow : EditorWindow
{
    const string WindowTitle = "Timeline Niveau Ski";
    const float RulerHeight = 22f;
    const float MusicTrackHeight = 48f;
    const float ObstacleTrackHeight = 56f;
    const float FlyingObstacleTrackHeight = 44f;
    const float TrackPadding = 8f;
    const float MinPixelsPerSecond = 20f;
    const float MaxPixelsPerSecond = 200f;
    const double PreviewFrameSeconds = 1.0 / 30.0;
    const double TimelineRepaintSeconds = 1.0 / 15.0;

    SkiLevelTimeline _timeline;
    SerializedObject _serializedTimeline;
    Vector2 _mainScroll;
    Vector2 _timelineScroll;
    float _pixelsPerSecond = 60f;
    float _playheadTime;
    bool _isPlaying;
    double _lastEditorTime;
    int _selectedIndex = -1;
    int _draggingIndex = -1;
    int _draggingTrimHandle;
    SkiObstacleType _placementType = SkiObstacleType.Rock;
    AudioClip _previewClip;
    AudioSource _previewSource;
    SkiDescentGame _previewGame;
    double _lastScenePreviewTime;
    double _lastTimelineRepaintTime;
    float _lastRenderedPlayhead = -1f;
    float _lastHeardClipTime = -1f;
    bool _previewSceneReady;
    bool _usingEditorAudioPreview;

    [MenuItem("LED/Éditeur Timeline Niveau")]
    public static void Open()
    {
        var consoleType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
        SkiLevelTimelineWindow window;

        if (consoleType != null)
            window = CreateWindow<SkiLevelTimelineWindow>(consoleType);
        else
            window = GetWindow<SkiLevelTimelineWindow>(false, WindowTitle, true);

        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(320, 280);
        window.Show();
    }

    [MenuItem("Assets/Create/LED/Niveau Timeline", false, 0)]
    static void CreateFromAssets()
    {
        var asset = CreateInstance<SkiLevelTimeline>();
        ProjectWindowUtil.CreateAsset(asset, "NouveauNiveau.asset");
    }

    void OnEnable()
    {
        titleContent = new GUIContent(WindowTitle);
        EditorApplication.update += OnEditorUpdate;
        _lastEditorTime = EditorApplication.timeSinceStartup;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        StopPreview(endScenePreview: true);
    }

    void OnEditorUpdate()
    {
        if (!_isPlaying || _timeline == null)
            return;

        var now = EditorApplication.timeSinceStartup;
        var delta = (float)(now - _lastEditorTime);
        _lastEditorTime = now;

        // Hors Play Unity, AudioSource.isPlaying peut être true sans que .time avance
        // → le playhead restait bloqué et la démo ne scrollait pas. On avance toujours
        // avec l'horloge éditeur ; on ne synchro audio que si le temps clip bouge vraiment.
        if (Application.isPlaying
            && _previewSource != null
            && _previewSource.isPlaying
            && _previewSource.time > _lastHeardClipTime + 0.0001f)
        {
            _lastHeardClipTime = _previewSource.time;
            if (_previewSource.time >= _timeline.MusicEnd)
            {
                _playheadTime = _timeline.MusicDuration;
                StopPreview();
                return;
            }

            _playheadTime = _timeline.ClipTimeToLevelTime(_previewSource.time);
        }
        else
            _playheadTime += delta;

        var duration = _timeline.Duration;
        if (_playheadTime >= duration)
        {
            _playheadTime = duration;
            StopPreview();
            return;
        }

        ApplyScenePreview();

        if (now - _lastTimelineRepaintTime >= TimelineRepaintSeconds)
        {
            _lastTimelineRepaintTime = now;
            Repaint();
            RepaintGameViews();
        }
    }

    public static void InvalidatePreviewRender()
    {
        var windows = Resources.FindObjectsOfTypeAll<SkiLevelTimelineWindow>();
        foreach (var window in windows)
        {
            window._lastRenderedPlayhead = -1f;
            window.ApplyScenePreview(force: true);
        }
    }

    void ApplyScenePreview(bool force = false)
    {
        // En Play Unity, le jeu possède l'écran : ne pas réécrire via la preview
        // (sinon home/game/preview se battent → clignotement).
        if (Application.isPlaying)
            return;

        if (_timeline == null)
            return;

        if (_previewGame == null)
            _previewGame = UnityEngine.Object.FindAnyObjectByType<SkiDescentGame>();

        if (_previewGame == null)
            return;

        if (!force && _isPlaying)
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastScenePreviewTime < PreviewFrameSeconds)
                return;
            _lastScenePreviewTime = now;
        }

        if (!force && Mathf.Approximately(_playheadTime, _lastRenderedPlayhead))
            return;

        _lastRenderedPlayhead = _playheadTime;
        EnsurePreviewSceneReady();
        if (_previewGame == null)
            return;

        _previewGame.PreviewTimeline(_timeline, _playheadTime);
        RepaintGameViews();
    }

    static void RepaintGameViews()
    {
        SceneView.RepaintAll();

        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
            return;

        foreach (var window in Resources.FindObjectsOfTypeAll(gameViewType))
        {
            if (window is EditorWindow editorWindow)
                editorWindow.Repaint();
        }
    }

    void EnsurePreviewSceneReady()
    {
        if (_previewGame == null)
            _previewGame = UnityEngine.Object.FindAnyObjectByType<SkiDescentGame>();

        if (_previewGame == null || _timeline == null)
            return;

        if (_previewSceneReady)
            return;

        _previewGame.BeginTimelinePreview(_timeline);
        _previewSceneReady = true;
        _lastRenderedPlayhead = -1f;
    }

    void ForceRebuildPreviewScene()
    {
        ResetPreviewScene();
        EnsurePreviewSceneReady();
    }

    void ResetPreviewScene()
    {
        _previewSceneReady = false;
        _lastRenderedPlayhead = -1f;
    }

    void InvalidatePreviewScene()
    {
        var shouldRefresh = _previewSceneReady || _isPlaying;
        ForceRebuildPreviewScene();
        if (shouldRefresh)
            ApplyScenePreview(force: true);
    }

    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);

        if (_timeline == null)
        {
            EditorGUILayout.HelpBox(
                "Sélectionne ou crée un asset Niveau Timeline.\n" +
                "Clic droit dans le Project > Create > LED > Niveau Timeline",
                MessageType.Info);
            return;
        }

        BindSerializedObject();

        _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
        DrawLevelSettings();
        EditorGUILayout.Space(6);
        DrawRecordMode();
        EditorGUILayout.Space(6);
        DrawTransport();
        EditorGUILayout.Space(4);
        DrawTimeline();
        EditorGUILayout.Space(6);
        DrawEventList();
        EditorGUILayout.EndScrollView();

        HandleKeyboard();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        var newTimeline = (SkiLevelTimeline)EditorGUILayout.ObjectField(
            _timeline, typeof(SkiLevelTimeline), false, GUILayout.MinWidth(200f));

        if (newTimeline != _timeline)
        {
            StopPreview(endScenePreview: true);
            _timeline = newTimeline;
            _serializedTimeline = null;
            _selectedIndex = -1;
            _playheadTime = 0f;
        }

        if (GUILayout.Button("Nouveau", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            CreateNewTimeline();

        GUILayout.FlexibleSpace();

        if (_timeline != null && GUILayout.Button("Assigner à la scène", EditorStyles.toolbarButton))
            AssignToScene();

        EditorGUILayout.EndHorizontal();
    }

    void DrawLevelSettings()
    {
        EditorGUILayout.LabelField("Niveau", EditorStyles.boldLabel);

        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUI.BeginChangeCheck();
            var music = (AudioClip)EditorGUILayout.ObjectField("Musique", _timeline.music, typeof(AudioClip), false);
            _timeline.runSpeed = EditorGUILayout.FloatField("Vitesse (runSpeed)", _timeline.runSpeed);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_timeline, "Modifier niveau");
                _timeline.music = music;
                _timeline.ClampMusicRange();
                EditorUtility.SetDirty(_timeline);
            }

            if (_timeline.music != null)
            {
                EditorGUI.BeginChangeCheck();
                var start = EditorGUILayout.FloatField("Début musique (s)", _timeline.musicStartTime);
                var end = EditorGUILayout.FloatField("Fin musique (s, 0 = fin)", _timeline.musicEndTime);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_timeline, "Couper musique");
                    _timeline.musicStartTime = start;
                    _timeline.musicEndTime = end;
                    _timeline.ClampMusicRange();
                    _playheadTime = Mathf.Clamp(_playheadTime, 0f, _timeline.Duration);
                    EditorUtility.SetDirty(_timeline);
                }

                EditorGUILayout.LabelField("Extrait utilisé", $"{_timeline.MusicDuration:F1} s");
            }

            EditorGUILayout.LabelField("Durée niveau", $"{_timeline.Duration:F1} s");
        }
    }

    void DrawRecordMode()
    {
        EditorGUILayout.LabelField("Mode enregistrement", EditorStyles.boldLabel);

        var game = UnityEngine.Object.FindAnyObjectByType<SkiDescentGame>();
        if (game == null)
        {
            EditorGUILayout.HelpBox(
                "Aucun SkiDescentGame dans la scène. Utilise « Assigner à la scène » d'abord.",
                MessageType.Warning);
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            var serializedGame = new SerializedObject(game);
            var recordProp = serializedGame.FindProperty("recordObstaclesOnJump");
            var typeProp = serializedGame.FindProperty("recordObstacleType");
            var useTimelineProp = serializedGame.FindProperty("useLevelTimeline");
            var timelineProp = serializedGame.FindProperty("levelTimeline");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(recordProp, new GUIContent("Enregistrer au saut"));
            EditorGUILayout.PropertyField(typeProp, new GUIContent("Type enregistré"));
            if (EditorGUI.EndChangeCheck())
            {
                useTimelineProp.boolValue = true;
                timelineProp.objectReferenceValue = _timeline;
                serializedGame.ApplyModifiedProperties();
                EditorUtility.SetDirty(game);
            }

            if (recordProp.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Lance Play dans Unity, puis saute (Espace) sur chaque temps fort.\n" +
                    "Chaque saut place un obstacle sous le skieur et l'ajoute à la timeline.\n" +
                    "Pas de collision en mode REC. Touches 1 = Roche, 2 = Sapin, 3 = Piaf.",
                    MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Effacer obstacles timeline"))
                {
                    Undo.RecordObject(_timeline, "Effacer obstacles");
                    _timeline.obstacles.Clear();
                    EditorUtility.SetDirty(_timeline);
                    _selectedIndex = -1;
                    InvalidatePreviewScene();
                }

                if (GUILayout.Button("Assigner + activer REC"))
                {
                    AssignToScene();
                    serializedGame.Update();
                    recordProp.boolValue = true;
                    useTimelineProp.boolValue = true;
                    timelineProp.objectReferenceValue = _timeline;
                    serializedGame.ApplyModifiedProperties();
                    EditorUtility.SetDirty(game);
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }

    void DrawTransport()
    {
        EditorGUILayout.BeginHorizontal();

        _placementType = (SkiObstacleType)EditorGUILayout.EnumPopup("Obstacle à placer", _placementType, GUILayout.Width(260f));

        GUILayout.FlexibleSpace();

        DrawCollisionToggle();

        if (Application.isPlaying)
        {
            if (GUILayout.Button(new GUIContent("■ Stop", "Stoppe la partie en cours (retour écran PRESS SPACE)."), GUILayout.Width(70f)))
                StopPlaySession();

            if (GUILayout.Button(new GUIContent("▶ Relancer", "Relance le niveau timeline depuis le début."), GUILayout.Width(80f)))
                RestartPlaySession();
        }
        else
        {
            if (GUILayout.Button(_isPlaying ? "■ Stop" : "▶ Play", GUILayout.Width(70f)))
            {
                if (_isPlaying)
                    StopPreview(endScenePreview: true);
                else
                    StartPreview();
            }

            if (GUILayout.Button(new GUIContent("⟲", "Remet le playhead à 0."), GUILayout.Width(28f)))
            {
                _playheadTime = 0f;
                ScrubPreview();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        _playheadTime = EditorGUILayout.Slider("Playhead", _playheadTime, 0f, _timeline.Duration);
        if (EditorGUI.EndChangeCheck())
        {
            if (_previewSource != null && _previewSource.isPlaying)
                _previewSource.time = GetSafeClipTime(_playheadTime);
            ScrubPreview();
        }
    }

    void DrawCollisionToggle()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<SkiDescentGame>();
        if (game == null)
        {
            GUI.enabled = false;
            GUILayout.Button("Collisions ?", GUILayout.Width(120f));
            GUI.enabled = true;
            return;
        }

        var on = game.CollisionsEnabled;
        var label = on ? "● Avec collisions" : "○ Sans collisions";
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = on ? new Color(0.55f, 0.85f, 0.55f) : new Color(0.85f, 0.7f, 0.35f);

        if (GUILayout.Button(new GUIContent(label, "Game over au contact des obstacles (utile en Play Unity)."), GUILayout.Width(140f)))
        {
            if (!Application.isPlaying)
                Undo.RecordObject(game, "Toggle collisions");

            game.CollisionsEnabled = !on;

            if (!Application.isPlaying)
                EditorUtility.SetDirty(game);
        }

        GUI.backgroundColor = prev;
    }

    void StopPlaySession()
    {
        StopPreview(endScenePreview: true);

        var game = UnityEngine.Object.FindAnyObjectByType<SkiDescentGame>();
        if (game == null)
            return;

        // Quitte le mode preview éditeur s'il était resté actif pendant Play
        // (sinon les collisions restent désactivées).
        if (game.IsEditorPreviewActive)
            game.EndTimelinePreview();
        else
            game.ShowHomeScreen();
    }

    void RestartPlaySession()
    {
        StopPreview(endScenePreview: true);

        var game = UnityEngine.Object.FindAnyObjectByType<SkiDescentGame>();
        if (game == null)
            return;

        if (_timeline != null)
        {
            game.LevelTimeline = _timeline;
            game.UseLevelTimeline = true;
        }

        if (game.IsEditorPreviewActive)
            game.EndTimelinePreview();

        game.StartGame();
        _playheadTime = 0f;
        Repaint();
    }

    void ScrubPreview()
    {
        PausePlaybackForScrub();
        ApplyScenePreview(force: true);
        Repaint();
    }

    void PausePlaybackForScrub()
    {
        _isPlaying = false;
        _lastRenderedPlayhead = -1f;
        StopPreviewAudio();
    }

    void DrawTimeline()
    {
        var duration = _timeline.Duration;
        var trackWidth = duration * _pixelsPerSecond;
        if (_timeline.music != null)
            trackWidth = Mathf.Max(trackWidth, _timeline.MusicDuration * _pixelsPerSecond);
        var totalHeight = RulerHeight + MusicTrackHeight + TrackPadding * 2f
            + ObstacleTrackHeight + TrackPadding + FlyingObstacleTrackHeight + TrackPadding;
        const float timelineViewportHeight = 220f;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Zoom", GUILayout.Width(40f));
        _pixelsPerSecond = GUILayout.HorizontalSlider(_pixelsPerSecond, MinPixelsPerSecond, MaxPixelsPerSecond);
        EditorGUILayout.LabelField($"{_pixelsPerSecond:F0} px/s", GUILayout.Width(60f));
        EditorGUILayout.EndHorizontal();

        _timelineScroll = EditorGUILayout.BeginScrollView(_timelineScroll, GUILayout.Height(timelineViewportHeight));

        var viewRect = GUILayoutUtility.GetRect(trackWidth + 40f, totalHeight);

        var trackOrigin = new Vector2(viewRect.x + 20f, viewRect.y + TrackPadding);
        var contentRect = new Rect(trackOrigin.x, trackOrigin.y, trackWidth, totalHeight);

        DrawBackground(contentRect);
        DrawRuler(contentRect, duration);
        DrawMusicTrack(contentRect);
        DrawObstacleTrack(contentRect, duration);
        DrawFlyingObstacleTrack(contentRect, duration);
        DrawPlayhead(contentRect, duration);

        HandleTimelineInput(contentRect, duration);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(
            "Musique : poignées jaunes | Sol : clic = roche/sapin | Vol : clic = piaf | " +
            "Alt+clic = placer même sur un obstacle | Suppr : supprimer",
            MessageType.None);
    }

    void DrawBackground(Rect contentRect)
    {
        EditorGUI.DrawRect(contentRect, new Color(0.16f, 0.16f, 0.18f));

        var beatWidth = _pixelsPerSecond;
        var beats = Mathf.CeilToInt(contentRect.width / beatWidth);
        for (var i = 0; i <= beats; i++)
        {
            var x = contentRect.x + i * beatWidth;
            var major = i % 5 == 0;
            var color = major ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.05f);
            EditorGUI.DrawRect(new Rect(x, contentRect.y, 1f, contentRect.height), color);
        }
    }

    void DrawRuler(Rect contentRect, float duration)
    {
        var rulerRect = new Rect(contentRect.x, contentRect.y, contentRect.width, RulerHeight);
        EditorGUI.DrawRect(rulerRect, new Color(0.22f, 0.22f, 0.24f));

        var step = GetRulerStep();
        for (var t = 0f; t <= duration + 0.001f; t += step)
        {
            var x = contentRect.x + t * _pixelsPerSecond;
            var height = Mathf.Approximately(t % (step * 5f), 0f) ? RulerHeight : RulerHeight * 0.5f;
            EditorGUI.DrawRect(new Rect(x, rulerRect.yMax - height, 1f, height), new Color(0.7f, 0.7f, 0.7f, 0.6f));

            if (height >= RulerHeight * 0.9f)
            {
                var label = $"{t:0.#}s";
                GUI.Label(new Rect(x + 3f, rulerRect.y + 2f, 50f, 16f), label, EditorStyles.miniLabel);
            }
        }
    }

    float GetRulerStep()
    {
        if (_pixelsPerSecond >= 120f) return 0.5f;
        if (_pixelsPerSecond >= 60f) return 1f;
        if (_pixelsPerSecond >= 30f) return 2f;
        return 5f;
    }

    void DrawMusicTrack(Rect contentRect)
    {
        var trackRect = new Rect(
            contentRect.x,
            contentRect.y + RulerHeight + TrackPadding,
            contentRect.width,
            MusicTrackHeight);

        EditorGUI.DrawRect(trackRect, new Color(0.12f, 0.28f, 0.42f, 0.9f));
        GUI.Label(new Rect(trackRect.x + 6f, trackRect.y + 4f, 80f, 16f), "Musique", EditorStyles.whiteMiniLabel);

        if (_timeline.music == null)
        {
            GUI.Label(new Rect(trackRect.x + 6f, trackRect.y + 22f, 300f, 16f),
                "Glisse un AudioClip ci-dessus", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        var musicWidth = _timeline.MusicDuration * _pixelsPerSecond;
        var musicRect = new Rect(trackRect.x, trackRect.y + 18f, musicWidth, trackRect.height - 22f);
        EditorGUI.DrawRect(musicRect, new Color(0.2f, 0.55f, 0.85f, 0.9f));
        DrawWaveformForRange(musicRect, _timeline.music, _timeline.MusicStart, _timeline.MusicEnd);

        DrawTrimHandle(musicRect.x, musicRect, true);
        DrawTrimHandle(musicRect.xMax, musicRect, false);

        var label = $"{_timeline.music.name}  [{_timeline.MusicStart:F1}s → {_timeline.MusicEnd:F1}s]";
        GUI.Label(new Rect(musicRect.x + 6f, musicRect.y + 4f, musicRect.width - 12f, 16f), label, EditorStyles.whiteMiniLabel);
    }

    void DrawTrimHandle(float x, Rect trackRect, bool isStart)
    {
        var handleRect = new Rect(x - 4f, trackRect.y, 8f, trackRect.height);
        EditorGUI.DrawRect(handleRect, new Color(1f, 0.85f, 0.2f, 0.95f));
        Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);
        var top = trackRect.y + 4f;
        var bottom = trackRect.yMax - 4f;
        if (isStart)
        {
            Handles.DrawAAConvexPolygon(
                new Vector3(x, top, 0f),
                new Vector3(x + 6f, top + 6f, 0f),
                new Vector3(x, top + 12f, 0f));
        }
        else
        {
            Handles.DrawAAConvexPolygon(
                new Vector3(x, top, 0f),
                new Vector3(x - 6f, top + 6f, 0f),
                new Vector3(x, top + 12f, 0f));
        }

        GUI.Label(handleRect, new GUIContent("", isStart ? "Début musique" : "Fin musique"));
    }

    void DrawWaveform(Rect rect, AudioClip clip)
    {
        DrawWaveformForRange(rect, clip, 0f, clip != null ? clip.length : 0f);
    }

    void DrawWaveformForRange(Rect rect, AudioClip clip, float startTime, float endTime)
    {
        if (clip == null || clip.samples <= 0 || rect.width <= 1f)
            return;

        var sampleRate = clip.frequency;
        var channels = clip.channels;
        var startFrame = Mathf.FloorToInt(startTime * sampleRate);
        var endFrame = Mathf.CeilToInt(endTime * sampleRate);
        var frameCount = Mathf.Max(1, endFrame - startFrame);
        var samples = new float[Mathf.Min(frameCount * channels, 16384)];

        if (!clip.GetData(samples, startFrame))
            return;

        var step = Mathf.Max(1, samples.Length / (int)rect.width);
        var centerY = rect.y + rect.height * 0.5f;
        var maxHeight = rect.height * 0.4f;

        Handles.color = new Color(1f, 1f, 1f, 0.35f);
        for (var x = 0; x < (int)rect.width; x++)
        {
            var sampleIndex = x * step;
            if (sampleIndex >= samples.Length)
                break;

            var amplitude = Mathf.Abs(samples[sampleIndex]);
            var h = amplitude * maxHeight;
            Handles.DrawLine(
                new Vector3(rect.x + x, centerY - h, 0f),
                new Vector3(rect.x + x, centerY + h, 0f));
        }
    }

    float GetGroundTrackTop(Rect contentRect)
    {
        return contentRect.y + RulerHeight + MusicTrackHeight + TrackPadding * 2f;
    }

    float GetFlyingTrackTop(Rect contentRect)
    {
        return GetGroundTrackTop(contentRect) + ObstacleTrackHeight + TrackPadding;
    }

    void DrawObstacleTrack(Rect contentRect, float duration)
    {
        var trackRect = new Rect(
            contentRect.x,
            GetGroundTrackTop(contentRect),
            contentRect.width,
            ObstacleTrackHeight);

        EditorGUI.DrawRect(trackRect, new Color(0.2f, 0.18f, 0.14f, 0.95f));
        GUI.Label(new Rect(trackRect.x + 6f, trackRect.y + 4f, 100f, 16f), "Sol", EditorStyles.whiteMiniLabel);

        var centerY = trackRect.y + trackRect.height * 0.55f;
        EditorGUI.DrawRect(new Rect(trackRect.x, centerY, trackRect.width, 1f), new Color(1f, 1f, 1f, 0.08f));

        DrawObstacleMarkers(contentRect, centerY, SkiObstacleLane.Ground, duration);
    }

    void DrawFlyingObstacleTrack(Rect contentRect, float duration)
    {
        var trackRect = new Rect(
            contentRect.x,
            GetFlyingTrackTop(contentRect),
            contentRect.width,
            FlyingObstacleTrackHeight);

        EditorGUI.DrawRect(trackRect, new Color(0.14f, 0.18f, 0.28f, 0.95f));
        GUI.Label(new Rect(trackRect.x + 6f, trackRect.y + 4f, 120f, 16f), "Piafs (vol)", EditorStyles.whiteMiniLabel);

        var centerY = trackRect.y + trackRect.height * 0.55f;
        EditorGUI.DrawRect(new Rect(trackRect.x, centerY, trackRect.width, 1f), new Color(0.5f, 0.7f, 1f, 0.12f));

        DrawObstacleMarkers(contentRect, centerY, SkiObstacleLane.Air, duration);
    }

    void DrawObstacleMarkers(Rect contentRect, float centerY, SkiObstacleLane lane, float duration)
    {
        _timeline.SortObstacles();
        for (var i = 0; i < _timeline.obstacles.Count; i++)
        {
            var evt = _timeline.obstacles[i];
            var eventLane = evt.lane;
            if (evt.type == SkiObstacleType.Piaf)
                eventLane = SkiObstacleLane.Air;

            if (eventLane != lane)
                continue;

            var x = contentRect.x + evt.timeSeconds * _pixelsPerSecond;
            var selected = i == _selectedIndex;
            var color = evt.type switch
            {
                SkiObstacleType.Sapin => new Color(0.3f, 0.75f, 0.35f),
                SkiObstacleType.Piaf => new Color(0.45f, 0.65f, 0.95f),
                _ => new Color(0.85f, 0.45f, 0.25f)
            };

            if (lane == SkiObstacleLane.Air)
                color = new Color(0.45f, 0.65f, 0.95f);

            if (selected)
                color = Color.Lerp(color, Color.white, 0.35f);

            var markerRect = new Rect(x - 7f, centerY - 14f, 14f, 28f);
            EditorGUI.DrawRect(markerRect, color);

            if (selected)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(new Vector3(x, centerY, 0f), Vector3.forward, 18f);
            }

            var tooltip = $"{evt.type} ({lane}) @ {evt.timeSeconds:F2}s";
            GUI.Label(new Rect(x - 30f, centerY + 16f, 60f, 14f), $"{evt.timeSeconds:F1}s", EditorStyles.centeredGreyMiniLabel);
            GUI.Label(markerRect, new GUIContent("", tooltip));
        }
    }

    void DrawPlayhead(Rect contentRect, float duration)
    {
        var x = contentRect.x + Mathf.Clamp(_playheadTime, 0f, duration) * _pixelsPerSecond;
        EditorGUI.DrawRect(new Rect(x, contentRect.y, 2f, contentRect.height), new Color(1f, 0.85f, 0.2f, 0.95f));
        Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);
        Handles.DrawAAConvexPolygon(
            new Vector3(x - 6f, contentRect.y, 0f),
            new Vector3(x + 6f, contentRect.y, 0f),
            new Vector3(x, contentRect.y + 10f, 0f));
    }

    void HandleTimelineInput(Rect contentRect, float duration)
    {
        var evt = Event.current;
        var musicTrackY = contentRect.y + RulerHeight + TrackPadding;
        var musicTrack = new Rect(contentRect.x, musicTrackY, contentRect.width, MusicTrackHeight);
        var groundTrackTop = GetGroundTrackTop(contentRect);
        var groundTrack = new Rect(contentRect.x, groundTrackTop, contentRect.width, ObstacleTrackHeight);
        var groundCenterY = groundTrack.y + groundTrack.height * 0.55f;
        var flyingTrackTop = GetFlyingTrackTop(contentRect);
        var flyingTrack = new Rect(contentRect.x, flyingTrackTop, contentRect.width, FlyingObstacleTrackHeight);
        var flyingCenterY = flyingTrack.y + flyingTrack.height * 0.55f;

        if (evt.type == EventType.ScrollWheel && contentRect.Contains(evt.mousePosition))
        {
            _pixelsPerSecond = Mathf.Clamp(
                _pixelsPerSecond - evt.delta.y * 4f,
                MinPixelsPerSecond,
                MaxPixelsPerSecond);
            evt.Use();
            Repaint();
            return;
        }

        if (evt.type == EventType.MouseDown && evt.button == 0)
        {
            if (_timeline.music != null)
            {
                var musicRect = new Rect(
                    contentRect.x,
                    musicTrack.y + 18f,
                    _timeline.MusicDuration * _pixelsPerSecond,
                    MusicTrackHeight - 22f);

                if (Mathf.Abs(evt.mousePosition.x - musicRect.x) < 8f && musicTrack.Contains(evt.mousePosition))
                {
                    _draggingTrimHandle = 1;
                    evt.Use();
                    return;
                }

                if (Mathf.Abs(evt.mousePosition.x - musicRect.xMax) < 8f && musicTrack.Contains(evt.mousePosition))
                {
                    _draggingTrimHandle = 2;
                    evt.Use();
                    return;
                }
            }

            if (IsNearPlayhead(evt.mousePosition, contentRect, duration))
            {
                _draggingIndex = -2;
                evt.Use();
                return;
            }

            var onFlyingTrack = flyingTrack.Contains(evt.mousePosition);
            var onGroundTrack = groundTrack.Contains(evt.mousePosition);
            var forcePlace = evt.alt;

            if (!forcePlace)
            {
                SkiObstacleLane? hitLane = null;
                if (onFlyingTrack)
                    hitLane = SkiObstacleLane.Air;
                else if (onGroundTrack)
                    hitLane = SkiObstacleLane.Ground;

                if (hitLane.HasValue)
                {
                    var hitIndex = HitTestObstacle(evt.mousePosition, contentRect, groundCenterY, flyingCenterY, hitLane);
                    if (hitIndex >= 0)
                    {
                        _selectedIndex = hitIndex;
                        _draggingIndex = hitIndex;
                        evt.Use();
                        Repaint();
                        return;
                    }
                }
            }

            if (onFlyingTrack)
            {
                var time = Mathf.Clamp((evt.mousePosition.x - contentRect.x) / _pixelsPerSecond, 0f, duration);
                Undo.RecordObject(_timeline, "Ajouter piaf");
                _timeline.AddObstacle(SnapTime(time), SkiObstacleType.Piaf, SkiObstacleLane.Air);
                _selectedIndex = _timeline.obstacles.Count - 1;
                EditorUtility.SetDirty(_timeline);
                InvalidatePreviewScene();
                evt.Use();
                Repaint();
                return;
            }

            if (onGroundTrack)
            {
                var time = Mathf.Clamp((evt.mousePosition.x - contentRect.x) / _pixelsPerSecond, 0f, duration);
                Undo.RecordObject(_timeline, "Ajouter obstacle");
                _timeline.AddObstacle(SnapTime(time), _placementType, SkiObstacleLane.Ground);
                _selectedIndex = _timeline.obstacles.Count - 1;
                EditorUtility.SetDirty(_timeline);
                InvalidatePreviewScene();
                evt.Use();
                Repaint();
            }
            else if (contentRect.Contains(evt.mousePosition))
            {
                _playheadTime = Mathf.Clamp((evt.mousePosition.x - contentRect.x) / _pixelsPerSecond, 0f, duration);
                if (_previewSource != null)
                    _previewSource.time = GetSafeClipTime(_playheadTime);
                ScrubPreview();
                evt.Use();
                Repaint();
            }
        }

        if (evt.type == EventType.MouseDrag && evt.button == 0)
        {
            if (_draggingTrimHandle == 1 && _timeline.music != null)
            {
                var delta = evt.delta.x / _pixelsPerSecond;
                Undo.RecordObject(_timeline, "Couper musique");
                _timeline.musicStartTime += delta;
                _timeline.ClampMusicRange();
                _playheadTime = Mathf.Clamp(_playheadTime, 0f, _timeline.Duration);
                EditorUtility.SetDirty(_timeline);
                evt.Use();
                Repaint();
            }
            else if (_draggingTrimHandle == 2 && _timeline.music != null)
            {
                var levelTime = Mathf.Clamp((evt.mousePosition.x - contentRect.x) / _pixelsPerSecond, 0.1f, _timeline.music.length);
                Undo.RecordObject(_timeline, "Couper musique");
                _timeline.musicEndTime = _timeline.MusicStart + levelTime;
                _timeline.ClampMusicRange();
                _playheadTime = Mathf.Clamp(_playheadTime, 0f, _timeline.Duration);
                EditorUtility.SetDirty(_timeline);
                evt.Use();
                Repaint();
            }
            else if (_draggingIndex == -2)
            {
                _playheadTime = Mathf.Clamp((evt.mousePosition.x - contentRect.x) / _pixelsPerSecond, 0f, duration);
                if (_previewSource != null)
                    _previewSource.time = GetSafeClipTime(_playheadTime);
                ScrubPreview();
                evt.Use();
                Repaint();
            }
            else if (_draggingIndex >= 0)
            {
                var time = Mathf.Clamp((evt.mousePosition.x - contentRect.x) / _pixelsPerSecond, 0f, duration);
                Undo.RecordObject(_timeline, "Déplacer obstacle");
                var obstacle = _timeline.obstacles[_draggingIndex];
                obstacle.timeSeconds = SnapTime(time);
                var type = obstacle.type;
                var lane = obstacle.lane;
                if (type == SkiObstacleType.Piaf)
                    lane = SkiObstacleLane.Air;

                _timeline.obstacles[_draggingIndex] = obstacle;
                _timeline.SortObstacles();
                _draggingIndex = FindIndexAtTime(obstacle.timeSeconds, type, lane);
                _selectedIndex = _draggingIndex;
                EditorUtility.SetDirty(_timeline);
                evt.Use();
                Repaint();
            }
        }

        if (evt.type == EventType.MouseUp && evt.button == 0)
        {
            _draggingIndex = -1;
            _draggingTrimHandle = 0;
        }
    }

    bool IsNearPlayhead(Vector2 mousePos, Rect contentRect, float duration)
    {
        var x = contentRect.x + _playheadTime * _pixelsPerSecond;
        return Mathf.Abs(mousePos.x - x) < 8f && contentRect.Contains(mousePos);
    }

    int HitTestObstacle(Vector2 mousePos, Rect contentRect, float groundCenterY, float flyingCenterY, SkiObstacleLane? laneFilter = null)
    {
        for (var i = _timeline.obstacles.Count - 1; i >= 0; i--)
        {
            var obstacle = _timeline.obstacles[i];
            var lane = obstacle.lane;
            if (obstacle.type == SkiObstacleType.Piaf)
                lane = SkiObstacleLane.Air;

            if (laneFilter.HasValue && lane != laneFilter.Value)
                continue;

            var centerY = lane == SkiObstacleLane.Air ? flyingCenterY : groundCenterY;
            var x = contentRect.x + obstacle.timeSeconds * _pixelsPerSecond;
            var markerRect = new Rect(x - 8f, centerY - 16f, 16f, 32f);
            if (markerRect.Contains(mousePos))
                return i;
        }

        return -1;
    }

    int FindIndexAtTime(float time, SkiObstacleType type, SkiObstacleLane lane)
    {
        for (var i = 0; i < _timeline.obstacles.Count; i++)
        {
            var obstacle = _timeline.obstacles[i];
            if (Mathf.Approximately(obstacle.timeSeconds, time)
                && obstacle.type == type
                && obstacle.lane == lane)
                return i;
        }

        return -1;
    }

    float SnapTime(float time)
    {
        if (Event.current != null && Event.current.control)
            return time;

        return Mathf.Round(time * 10f) / 10f;
    }

    void DrawEventList()
    {
        EditorGUILayout.LabelField("Liste des obstacles", EditorStyles.boldLabel);

        if (_timeline.obstacles.Count == 0)
        {
            EditorGUILayout.LabelField("Aucun obstacle placé.", EditorStyles.miniLabel);
            return;
        }

        _timeline.SortObstacles();
        for (var i = 0; i < _timeline.obstacles.Count; i++)
        {
            var obstacle = _timeline.obstacles[i];
            EditorGUILayout.BeginHorizontal();

            var selected = i == _selectedIndex;
            if (GUILayout.Toggle(selected, "", GUILayout.Width(16f)) != selected)
                _selectedIndex = i;

            EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(24f));
            EditorGUILayout.LabelField($"{obstacle.timeSeconds:F2} s", GUILayout.Width(56f));
            var laneLabel = obstacle.lane == SkiObstacleLane.Air ? "Piaf" : "Sol";
            EditorGUILayout.LabelField($"{obstacle.type} ({laneLabel})", GUILayout.Width(100f));

            if (GUILayout.Button("▶", GUILayout.Width(24f)))
            {
                _playheadTime = obstacle.timeSeconds;
                _selectedIndex = i;
            }

            if (GUILayout.Button("✕", GUILayout.Width(24f)))
            {
                Undo.RecordObject(_timeline, "Supprimer obstacle");
                _timeline.RemoveAt(i);
                if (_selectedIndex >= _timeline.obstacles.Count)
                    _selectedIndex = _timeline.obstacles.Count - 1;
                EditorUtility.SetDirty(_timeline);
                InvalidatePreviewScene();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    void HandleKeyboard()
    {
        var evt = Event.current;
        if (evt.type != EventType.KeyDown)
            return;

        if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
        {
            if (_selectedIndex >= 0 && _selectedIndex < _timeline.obstacles.Count)
            {
                Undo.RecordObject(_timeline, "Supprimer obstacle");
                _timeline.RemoveAt(_selectedIndex);
                _selectedIndex = Mathf.Min(_selectedIndex, _timeline.obstacles.Count - 1);
                EditorUtility.SetDirty(_timeline);
                InvalidatePreviewScene();
                evt.Use();
                Repaint();
            }
        }
    }

    void BindSerializedObject()
    {
        if (_serializedTimeline == null || _serializedTimeline.targetObject != _timeline)
            _serializedTimeline = new SerializedObject(_timeline);
    }

    void CreateNewTimeline()
    {
        var path = EditorUtility.SaveFilePanelInProject(
            "Créer un niveau",
            "NouveauNiveau",
            "asset",
            "Choisis où sauvegarder le niveau");

        if (string.IsNullOrEmpty(path))
            return;

        var asset = CreateInstance<SkiLevelTimeline>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        _timeline = asset;
        _serializedTimeline = null;
        _selectedIndex = -1;
        _playheadTime = 0f;
        Selection.activeObject = asset;
    }

    void AssignToScene()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Mode Play",
                "Arrête le mode Play avant d'assigner la timeline à la scène.",
                "OK");
            return;
        }

        var game = UnityEngine.Object.FindAnyObjectByType<SkiDescentGame>();
        if (game == null)
        {
            EditorUtility.DisplayDialog(
                "Scène",
                "Aucun SkiDescentGame dans la scène.\nUtilise LED > Configurer la scène d'abord.",
                "OK");
            return;
        }

        Undo.RecordObject(game, "Assigner timeline");
        var serialized = new SerializedObject(game);
        serialized.FindProperty("levelTimeline").objectReferenceValue = _timeline;
        serialized.FindProperty("useLevelTimeline").boolValue = true;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);

        EditorUtility.DisplayDialog("Assigné", $"Timeline assignée à {game.name}.", "OK");
    }

    void StartPreview()
    {
        // En Play Unity, on relance la vraie partie (pas la preview éditeur,
        // qui bloquait les collisions via _editorPreviewActive).
        if (Application.isPlaying)
        {
            RestartPlaySession();
            return;
        }

        if (UnityEngine.Object.FindAnyObjectByType<SkiDescentGame>() == null)
        {
            EditorUtility.DisplayDialog(
                "Preview",
                "Aucun SkiDescentGame dans la scène.\nUtilise LED > Configurer la scène, puis réessaie.",
                "OK");
            return;
        }

        _isPlaying = true;
        _lastEditorTime = EditorApplication.timeSinceStartup;
        _lastScenePreviewTime = 0;
        _lastHeardClipTime = -1f;
        _lastRenderedPlayhead = -1f;
        ForceRebuildPreviewScene();
        ApplyScenePreview(force: true);

        if (_timeline.music == null)
            return;

        StartPreviewAudio(GetSafeClipTime(_playheadTime));
    }

    void StopPreview(bool endScenePreview = false)
    {
        _isPlaying = false;
        StopPreviewAudio();

        if (endScenePreview)
        {
            if (_previewGame == null)
                _previewGame = UnityEngine.Object.FindAnyObjectByType<SkiDescentGame>();

            if (_previewGame != null)
                _previewGame.EndTimelinePreview();

            ResetPreviewScene();
        }
    }

    void StartPreviewAudio(float clipTimeSeconds)
    {
        StopPreviewAudio();

        if (_timeline == null || _timeline.music == null)
            return;

        // Hors Play Unity : AudioUtil (sinon le playhead / la musique restent figés).
        if (!Application.isPlaying)
        {
            _usingEditorAudioPreview = TryPlayEditorClip(_timeline.music, clipTimeSeconds);
            if (_usingEditorAudioPreview)
                return;
        }

        EnsurePreviewSource();
        if (_previewClip != _timeline.music)
        {
            _previewClip = _timeline.music;
            _previewSource.clip = _timeline.music;
        }

        _previewSource.time = clipTimeSeconds;
        _previewSource.Play();
        _lastHeardClipTime = _previewSource.time;
        _usingEditorAudioPreview = false;
    }

    void StopPreviewAudio()
    {
        if (_usingEditorAudioPreview)
        {
            StopEditorClips();
            _usingEditorAudioPreview = false;
        }

        if (_previewSource != null && _previewSource.isPlaying)
            _previewSource.Stop();
    }

    void EnsurePreviewSource()
    {
        if (_previewSource != null)
            return;

        var host = EditorUtility.CreateGameObjectWithHideFlags(
            "SkiTimelinePreviewAudio",
            HideFlags.HideAndDontSave);
        _previewSource = host.AddComponent<AudioSource>();
        _previewSource.playOnAwake = false;
        _previewSource.hideFlags = HideFlags.HideAndDontSave;
    }

    static bool TryPlayEditorClip(AudioClip clip, float startTimeSeconds)
    {
        var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtil == null || clip == null)
            return false;

        StopEditorClips();

        var startSample = Mathf.Clamp(
            Mathf.FloorToInt(startTimeSeconds * clip.frequency),
            0,
            Mathf.Max(0, clip.samples - 1));

        // Unity 2022+/6 : PlayPreviewClip(AudioClip, int startSample, bool loop)
        var playMethod = audioUtil.GetMethod(
            "PlayPreviewClip",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
            null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null);

        if (playMethod == null)
            return false;

        playMethod.Invoke(null, new object[] { clip, startSample, false });
        return true;
    }

    static void StopEditorClips()
    {
        var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        var stopMethod = audioUtil?.GetMethod(
            "StopAllPreviewClips",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        stopMethod?.Invoke(null, null);
    }

    float GetSafeClipTime(float levelTimeSeconds)
    {
        if (_timeline == null || _timeline.music == null)
            return 0f;

        var clipLen = _timeline.music.length;
        if (clipLen <= 0.0001f)
            return 0f;

        var t = _timeline.LevelTimeToClipTime(levelTimeSeconds);
        // Unity AudioSource.time: keep strictly below clip length to avoid invalid seek.
        var max = clipLen - 0.001f;
        if (max < 0f)
            max = 0f;
        return Mathf.Clamp(t, 0f, max);
    }
}
#endif
