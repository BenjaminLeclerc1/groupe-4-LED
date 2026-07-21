using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(LEDWallSimulator))]
public class SkiDescentGame : MonoBehaviour
{
    struct Obstacle
    {
        public float WorldX;
        public bool Scored;
        public SkiObstacleType Type;
        public SkiObstacleLane Lane;
    }

    [Header("Sprites")]
    [SerializeField] Texture2D obstacleTexture;
    [SerializeField] Texture2D sapinTexture;
    [SerializeField] Texture2D piafTexture;

    [Header("Niveau")]
    [SerializeField] SkiLevelTimeline levelTimeline;
    [SerializeField] bool useLevelTimeline;

    [Header("Enregistrement")]
    [Tooltip("Mode test : pas d'obstacles au départ, chaque saut en place un devant le skieur.")]
    [SerializeField] bool recordObstaclesOnJump;
    [SerializeField] SkiObstacleType recordObstacleType = SkiObstacleType.Rock;
    [Tooltip("Distance devant le skieur (pixels) où l'obstacle est posé.")]
    [SerializeField] int recordObstacleAheadOffset = 32;

    [Header("Gameplay")]
    [SerializeField] float runSpeed = 34f;
    [SerializeField] float jumpForce = 77f;
    [SerializeField] float gravity = 125f;
    [SerializeField] float verticalMoveSpeed = 55f;
    [Tooltip("Pixels sous le haut de tête au sommet du saut — plus grand = piaf plus bas, plus facile à toucher.")]
    [SerializeField] int flyingObstacleExtraDrop = 10;
    [SerializeField] int maxPlayerAltitude = 96;
    [SerializeField] int playerColumn = 22;
    [SerializeField] float minObstacleSpacing = 42f;
    [SerializeField] float maxObstacleSpacing = 78f;
    [SerializeField] bool animateInEditMode;

    LEDWallSimulator _wall;
    SpritePixelData _skier;
    SpritePixelData _obstacle;
    SpritePixelData _sapin;
    SpritePixelData _piaf;

    readonly List<Obstacle> _obstacles = new();
    float _scrollOffset;
    float _playerY;
    float _velocityY;
    float _nextSpawnWorldX;
    bool _isRunning;
    bool _isGameOver;
    bool _isAtHomeScreen = true;
    bool _useTimelineMode;
    float _blinkTimer;
    float _groundY;
    float _jumpImpulse;
    int _score;
    AudioSource _musicSource;
    float _activeRunSpeed;
    bool _editorPreviewActive;
    SkiLevelTimeline _editorPreviewTimeline;

    const float BlinkInterval = 0.5f;
    const string PromptText = "PRESS SPACE";
    const int PromptScale = 2;
    const int PromptMarginFromBottom = 20;

    void OnEnable()
    {
        _wall = GetComponent<LEDWallSimulator>();
        if (!_editorPreviewActive)
            ShowHomeScreen();
    }

    void OnDisable()
    {
        _isRunning = false;
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        if (_wall == null)
            _wall = GetComponent<LEDWallSimulator>();

        if (_wall == null || _isAtHomeScreen)
            return;

        LoadSprites();
        RenderFrame();
    }

    public void ShowHomeScreen()
    {
        if (_wall == null)
            _wall = GetComponent<LEDWallSimulator>();

        LoadSprites();

        _isAtHomeScreen = true;
        _isRunning = false;
        _isGameOver = false;
        _useTimelineMode = false;
        _blinkTimer = 0f;
        StopLevelMusic();
        _groundY = GetPlayerGroundY();
        _playerY = _groundY;

        if (_skier.HasPixels)
            RenderHomeScreen();
    }

    public void StartGame()
    {
        if (_wall == null)
            _wall = GetComponent<LEDWallSimulator>();

        LoadSprites();

        _isAtHomeScreen = false;
        _scrollOffset = 0f;
        _velocityY = 0f;
        _isGameOver = false;
        _score = 0;
        _obstacles.Clear();
        _nextSpawnWorldX = 90f;
        _useTimelineMode = (useLevelTimeline || recordObstaclesOnJump) && levelTimeline != null;
        _activeRunSpeed = _useTimelineMode ? levelTimeline.runSpeed : runSpeed;
        _groundY = GetPlayerGroundY();
        _playerY = _groundY;
        var groundDrop = (SkiSlopeRenderer.PlayerGroundTop + 1f) - _groundY;
        _jumpImpulse = jumpForce + groundDrop;
        _isRunning = _skier.HasPixels;

        if (_useTimelineMode && !recordObstaclesOnJump)
            LoadTimelineObstacles();
        else if (!_useTimelineMode && _isRunning && _obstacle.HasPixels)
            SpawnObstacle(_nextSpawnWorldX, SkiObstacleType.Rock);

        if (_useTimelineMode)
            StartLevelMusic();
        else
            StopLevelMusic();

        if (_isRunning)
            RenderFrame();
    }

    void LoadTimelineObstacles()
    {
        foreach (var evt in levelTimeline.GetSortedEvents())
        {
            var worldX = SkiLevelTimeline.TimeToWorldX(evt.timeSeconds, playerColumn, _activeRunSpeed);
            SpawnObstacle(worldX, evt.type, evt.lane);
        }
    }

    void StartLevelMusic()
    {
        if (levelTimeline.music == null)
            return;

        EnsureMusicSource();
        _musicSource.clip = levelTimeline.music;
        _musicSource.time = levelTimeline.MusicStart;
        _musicSource.Play();
    }

    void StopLevelMusic()
    {
        if (_musicSource == null)
            return;

        _musicSource.Stop();
        _musicSource.clip = null;
    }

    void EnsureMusicSource()
    {
        if (_musicSource != null)
            return;

        _musicSource = GetComponent<AudioSource>();
        if (_musicSource == null)
            _musicSource = gameObject.AddComponent<AudioSource>();

        _musicSource.playOnAwake = false;
        _musicSource.loop = false;
    }

    public void ForceRender()
    {
        if (_wall == null)
            _wall = GetComponent<LEDWallSimulator>();

        LoadSprites();

        if (_isAtHomeScreen)
        {
            if (_skier.HasPixels)
                RenderHomeScreen();
            return;
        }

        if (!_isRunning && _skier.HasPixels)
            _isRunning = true;

        if (_skier.HasPixels)
            RenderFrame();
    }

    void Update()
    {
        if (_isAtHomeScreen)
        {
            UpdateHomeScreen();
            return;
        }

        if (!_isRunning)
            return;

        if (!Application.isPlaying)
        {
            if (_editorPreviewActive)
                return;

            if (animateInEditMode)
            {
                _scrollOffset += runSpeed * 0.02f;
                RenderFrame();
            }

            return;
        }

        if (_isGameOver)
        {
            _blinkTimer += Time.deltaTime;
            RenderDeathScreen();

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                StartGame();

            return;
        }

        HandleJumpInput();
        HandleRecordInput();
        HandleVerticalInput();
        UpdatePhysics();
        UpdateWorld();
        UpdateScore();
        CheckCollisions();
        RenderFrame();
    }

    void UpdateScore()
    {
        for (var i = 0; i < _obstacles.Count; i++)
        {
            var obstacle = _obstacles[i];
            if (obstacle.Scored)
                continue;

            var screenX = Mathf.FloorToInt(obstacle.WorldX - _scrollOffset);
            if (screenX + GetObstacleSprite(obstacle.Type).Width >= playerColumn)
                continue;

            obstacle.Scored = true;
            _obstacles[i] = obstacle;
            _score++;
        }
    }

    void UpdateHomeScreen()
    {
        if (!Application.isPlaying)
        {
            if (animateInEditMode)
                RenderHomeScreen();

            return;
        }

        _blinkTimer += Time.deltaTime;
        RenderHomeScreen();

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            StartGame();
    }

    void RenderHomeScreen()
    {
        var buffer = _wall.Buffer;
        buffer.ClearPixels(SkiSlopeRenderer.Sky);
        SkiSlopeRenderer.Draw(buffer, 0f, playerColumn);

        if (_skier.HasPixels)
        {
            buffer.DrawTexture(
                _skier.Pixels,
                _skier.Width,
                _skier.Height,
                new Vector2Int(playerColumn, Mathf.FloorToInt(_groundY)),
                applyAfter: false);
        }

        // originY is the text's bottom edge measured from the bottom of the
        // screen (row 0): near the top of the screen, well clear of the
        // skier/slope near the bottom.
        int homePromptY = LEDWallConfig.VisibleHeight - PromptMarginFromBottom - PixelFont.GlyphHeight * PromptScale;
        DrawBlinkingPrompt(buffer, homePromptY, PromptScale, Color.yellow);
        buffer.Apply();
        _wall.UpdatePreviews();
    }

    static readonly Color SubtlePromptColor = new(0.55f, 0.55f, 0.55f);

    void RenderDeathScreen()
    {
        var buffer = _wall.Buffer;
        buffer.ClearPixels(SkiSlopeRenderer.Sky);
        SkiSlopeRenderer.Draw(buffer, 0f, playerColumn);

        const string title = "GAME OVER";
        const int titleScale = 2;
        int titleWidth = PixelFont.MeasureWidth(title, titleScale);
        int titleX = (LEDWallConfig.VisibleWidth - titleWidth) / 2;
        int titleY = LEDWallConfig.VisibleHeight - 24 - PixelFont.GlyphHeight * titleScale;
        PixelFont.Draw(title, titleX, titleY, Color.red, titleScale, buffer.SetPixel);

        string scoreText = "SCORE " + _score;
        int scoreWidth = PixelFont.MeasureWidth(scoreText, 1);
        int scoreX = (LEDWallConfig.VisibleWidth - scoreWidth) / 2;
        int scoreY = titleY - 6 - PixelFont.GlyphHeight;
        PixelFont.Draw(scoreText, scoreX, scoreY, Color.white, 1, buffer.SetPixel);

        // Small, dim and near the bottom - a secondary hint, not competing
        // with the title/score for attention.
        const int deathPromptY = 10;
        DrawBlinkingPrompt(buffer, deathPromptY, 1, SubtlePromptColor);

        buffer.Apply();
        _wall.UpdatePreviews();
    }

    void DrawBlinkingPrompt(LEDWallBuffer buffer, int originY, int scale, Color color)
    {
        bool visible = Mathf.FloorToInt(_blinkTimer / BlinkInterval) % 2 == 0;
        if (!visible)
            return;

        int textWidth = PixelFont.MeasureWidth(PromptText, scale);
        int originX = (LEDWallConfig.VisibleWidth - textWidth) / 2;

        PixelFont.Draw(PromptText, originX, originY, color, scale, buffer.SetPixel);
    }

    void DrawScoreHud(LEDWallBuffer buffer)
    {
        string scoreText = _score.ToString();
        const int margin = 4;
        int originY = LEDWallConfig.VisibleHeight - margin - PixelFont.GlyphHeight;
        PixelFont.Draw(scoreText, margin, originY, Color.white, 1, buffer.SetPixel);
    }

    void HandleJumpInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
            return;

        if (!IsGrounded())
            return;

        _velocityY = _jumpImpulse;

        if (recordObstaclesOnJump && _useTimelineMode && levelTimeline != null)
            RecordObstacleAtCurrentTime();
    }

    void HandleRecordInput()
    {
        if (!recordObstaclesOnJump || !_useTimelineMode)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            recordObstacleType = SkiObstacleType.Rock;
        else if (keyboard.digit2Key.wasPressedThisFrame)
            recordObstacleType = SkiObstacleType.Sapin;
        else if (keyboard.digit3Key.wasPressedThisFrame)
            recordObstacleType = SkiObstacleType.Piaf;
    }

    void HandleVerticalInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        var move = 0f;
        if (keyboard.zKey.isPressed)
            move += 1f;
        if (keyboard.sKey.isPressed)
            move -= 1f;

        if (Mathf.Approximately(move, 0f))
            return;

        _playerY += move * verticalMoveSpeed * Time.deltaTime;
        _playerY = Mathf.Clamp(_playerY, _groundY, GetMaxPlayerY());

        if (_playerY <= _groundY + 0.01f)
        {
            _playerY = _groundY;
            if (move < 0f)
                _velocityY = 0f;
        }
        else if (move > 0f && _velocityY < 0f)
            _velocityY = 0f;
    }

    void UpdatePhysics()
    {
        _velocityY -= gravity * Time.deltaTime;
        _playerY += _velocityY * Time.deltaTime;

        if (_playerY <= _groundY)
        {
            _playerY = _groundY;
            _velocityY = 0f;
        }
    }

    void UpdateWorld()
    {
        if (_useTimelineMode && _musicSource != null && _musicSource.isPlaying)
        {
            var levelTime = levelTimeline.ClipTimeToLevelTime(_musicSource.time);
            _scrollOffset = levelTime * _activeRunSpeed;

            if (_musicSource.time >= levelTimeline.MusicEnd)
                _musicSource.Stop();
        }
        else if (_useTimelineMode && levelTimeline != null && levelTimeline.music == null)
            _scrollOffset += _activeRunSpeed * Time.deltaTime;
        else if (!_useTimelineMode)
            _scrollOffset += _activeRunSpeed * Time.deltaTime;

        if (_useTimelineMode)
        {
            CleanupOffscreenObstacles();
            return;
        }

        while (_obstacle.HasPixels && _nextSpawnWorldX < _scrollOffset + LEDWallConfig.VisibleWidth + 40f)
        {
            _nextSpawnWorldX += Random.Range(minObstacleSpacing, maxObstacleSpacing);
            SpawnObstacle(_nextSpawnWorldX, SkiObstacleType.Rock);
        }

        CleanupOffscreenObstacles();
    }

    void CleanupOffscreenObstacles()
    {
        if (_editorPreviewActive)
            return;

        var removeWidth = GetWidestObstacleWidth() + 8;
        var removeBefore = -removeWidth;
        for (var i = _obstacles.Count - 1; i >= 0; i--)
        {
            if (_obstacles[i].WorldX - _scrollOffset < removeBefore)
                _obstacles.RemoveAt(i);
        }
    }

    int GetWidestObstacleWidth()
    {
        var width = _obstacle.Width;
        if (_sapin.Width > width)
            width = _sapin.Width;
        if (_piaf.Width > width)
            width = _piaf.Width;
        return width;
    }

    void SpawnObstacle(float worldX, SkiObstacleType type, SkiObstacleLane lane = SkiObstacleLane.Ground)
    {
        if (type == SkiObstacleType.Piaf)
            lane = SkiObstacleLane.Air;

        _obstacles.Add(new Obstacle { WorldX = worldX, Type = type, Lane = lane });
    }

    SpritePixelData GetObstacleSprite(SkiObstacleType type)
    {
        if (type == SkiObstacleType.Piaf && _piaf.HasPixels)
            return _piaf;

        if (type == SkiObstacleType.Sapin && _sapin.HasPixels)
            return _sapin;

        return _obstacle;
    }

    void CheckCollisions()
    {
        if (recordObstaclesOnJump || _editorPreviewActive || !_skier.HasPixels)
            return;

        var playerLeft = playerColumn + _skier.HitMinX;
        var playerRight = playerColumn + _skier.HitMaxX;
        var playerBottom = Mathf.FloorToInt(_playerY) + _skier.HitMinY;
        var playerTop = Mathf.FloorToInt(_playerY) + _skier.HitMaxY;

        foreach (var obstacle in _obstacles)
        {
            var sprite = GetObstacleSprite(obstacle.Type);
            if (!sprite.HasPixels)
                continue;

            var screenX = Mathf.FloorToInt(obstacle.WorldX - _scrollOffset);
            var obstacleScreenY = GetObstacleScreenY(screenX, sprite, obstacle.Type, obstacle.Lane);
            var obstacleLeft = screenX + sprite.HitMinX;
            var obstacleRight = screenX + sprite.HitMaxX;
            var obstacleBottom = obstacleScreenY + sprite.HitMinY;
            var obstacleTop = obstacleScreenY + sprite.HitMaxY;

            if (playerRight < obstacleLeft || playerLeft > obstacleRight)
                continue;

            if (playerBottom >= obstacleTop - 4 && !IsAirObstacle(obstacle.Type, obstacle.Lane))
                continue;

            _isGameOver = true;
            _blinkTimer = 0f;
            StopLevelMusic();
            return;
        }
    }

    void RenderFrame()
    {
        var buffer = _wall.Buffer;
        buffer.ClearPixels(SkiSlopeRenderer.Sky);
        SkiSlopeRenderer.Draw(buffer, _scrollOffset, playerColumn);
        DrawObstacles(buffer);

        if (_skier.HasPixels)
        {
            buffer.DrawTexture(
                _skier.Pixels,
                _skier.Width,
                _skier.Height,
                new Vector2Int(playerColumn, Mathf.FloorToInt(_playerY)),
                applyAfter: false);
        }

        DrawScoreHud(buffer);
        if (recordObstaclesOnJump)
            DrawRecordHud(buffer);
        buffer.Apply();
        _wall.UpdatePreviews();
    }

    void DrawRecordHud(LEDWallBuffer buffer)
    {
        const string label = "REC";
        const int scale = 1;
        int originX = LEDWallConfig.VisibleWidth - PixelFont.MeasureWidth(label, scale) - 4;
        int originY = LEDWallConfig.VisibleHeight - 4 - PixelFont.GlyphHeight;
        PixelFont.Draw(label, originX, originY, Color.red, scale, buffer.SetPixel);
    }

    float GetCurrentLevelTime()
    {
        if (_useTimelineMode && levelTimeline != null && _musicSource != null && _musicSource.clip != null)
            return levelTimeline.ClipTimeToLevelTime(_musicSource.time);

        return _scrollOffset / _activeRunSpeed;
    }

    void RecordObstacleAtCurrentTime()
    {
        var ahead = Mathf.Max(0, recordObstacleAheadOffset);
        var levelTime = GetCurrentLevelTime() + ahead / Mathf.Max(0.01f, _activeRunSpeed);
        levelTimeline.AddObstacle(levelTime, recordObstacleType, GetRecordLane());
        var worldX = SkiLevelTimeline.TimeToWorldX(levelTime, playerColumn, _activeRunSpeed);
        SpawnObstacle(worldX, recordObstacleType, GetRecordLane());
        MarkTimelineDirty();
    }

    SkiObstacleLane GetRecordLane()
    {
        return recordObstacleType == SkiObstacleType.Piaf
            ? SkiObstacleLane.Air
            : SkiObstacleLane.Ground;
    }

    void MarkTimelineDirty()
    {
#if UNITY_EDITOR
        if (levelTimeline != null)
            UnityEditor.EditorUtility.SetDirty(levelTimeline);
#endif
    }

    public bool RecordObstaclesOnJump
    {
        get => recordObstaclesOnJump;
        set => recordObstaclesOnJump = value;
    }

    public SkiObstacleType RecordObstacleType
    {
        get => recordObstacleType;
        set => recordObstacleType = value;
    }

    void DrawObstacles(LEDWallBuffer buffer)
    {
        foreach (var obstacle in _obstacles)
        {
            var sprite = GetObstacleSprite(obstacle.Type);
            if (!sprite.HasPixels)
                continue;

            var screenX = Mathf.FloorToInt(obstacle.WorldX - _scrollOffset);
            if (screenX < -sprite.Width || screenX >= LEDWallConfig.VisibleWidth)
                continue;

            var obstacleScreenY = GetObstacleScreenY(screenX, sprite, obstacle.Type, obstacle.Lane);
            buffer.DrawTexture(
                sprite.Pixels,
                sprite.Width,
                sprite.Height,
                new Vector2Int(screenX, obstacleScreenY),
                applyAfter: false);
        }
    }

    void LoadSprites()
    {
        var skierTexture = _wall.SourceTexture != null
            ? _wall.SourceTexture
            : LEDSpriteLoader.LoadSkieur();

        var rockTexture = obstacleTexture != null
            ? obstacleTexture
            : LEDSpriteLoader.LoadObstacle();

        var treeTexture = sapinTexture != null
            ? sapinTexture
            : LEDSpriteLoader.LoadSapin();

        var piafTex = piafTexture != null
            ? piafTexture
            : LEDSpriteLoader.LoadPiaf();

        _skier = SpritePixelData.FromTexture(skierTexture);
        _obstacle = SpritePixelData.FromTexture(rockTexture, stripBlack: false);
        _sapin = treeTexture != null
            ? SpritePixelData.FromTexture(treeTexture, stripBlack: false)
            : default;
        _piaf = piafTex != null
            ? SpritePixelData.FromTexture(piafTex, stripBlack: false)
            : default;
    }

    bool IsGrounded()
    {
        return Mathf.Abs(_playerY - _groundY) < 0.5f && Mathf.Abs(_velocityY) < 0.1f;
    }

    int GetGroundTopAtScreen(int screenColumn)
    {
        return SkiSlopeRenderer.GetScreenGroundTop(screenColumn, playerColumn);
    }

    static bool IsAirObstacle(SkiObstacleType type, SkiObstacleLane lane)
    {
        return type == SkiObstacleType.Piaf || lane == SkiObstacleLane.Air;
    }

    int GetObstacleScreenY(int screenColumn, SpritePixelData sprite, SkiObstacleType type, SkiObstacleLane lane)
    {
        if (IsAirObstacle(type, lane))
            return GetFlyingObstacleScreenY(sprite);

        return GetGroundTopAtScreen(screenColumn) - sprite.HitMinY;
    }

    int GetFlyingObstacleScreenY(SpritePixelData sprite)
    {
        EnsureJumpImpulseForHeight();
        var skierHeadTop = _skier.HasPixels ? _skier.HitMaxY : 0;
        var targetBottom = GetJumpApexY() + skierHeadTop - flyingObstacleExtraDrop;
        return Mathf.RoundToInt(targetBottom) - sprite.HitMinY;
    }

    void EnsureJumpImpulseForHeight()
    {
        if (_jumpImpulse > 0f && _groundY > 0f)
            return;

        _groundY = GetPlayerGroundY();
        var groundDrop = (SkiSlopeRenderer.PlayerGroundTop + 1f) - _groundY;
        _jumpImpulse = jumpForce + groundDrop;
    }

    float GetJumpApexY()
    {
        return _groundY + (_jumpImpulse * _jumpImpulse) / (2f * gravity);
    }

    float GetMaxPlayerY()
    {
        if (!_skier.HasPixels)
            return maxPlayerAltitude;

        return maxPlayerAltitude - _skier.HitMinY;
    }

    float GetPlayerGroundY()
    {
        return GetGroundTopAtScreen(playerColumn) - _skier.HitMinY;
    }

    public void BeginTimelinePreview(SkiLevelTimeline timeline)
    {
        if (_wall == null)
            _wall = GetComponent<LEDWallSimulator>();

        if (timeline == null || _wall == null)
            return;

        LoadSprites();

        _editorPreviewActive = true;
        _editorPreviewTimeline = timeline;
        levelTimeline = timeline;
        _isAtHomeScreen = false;
        _isRunning = true;
        _isGameOver = false;
        _useTimelineMode = true;
        _activeRunSpeed = timeline.runSpeed;
        _groundY = GetPlayerGroundY();
        _playerY = _groundY;
        _velocityY = 0f;
        _score = 0;
        var groundDrop = (SkiSlopeRenderer.PlayerGroundTop + 1f) - _groundY;
        _jumpImpulse = jumpForce + groundDrop;

        ReloadPreviewObstacles();
    }

    void ReloadPreviewObstacles()
    {
        if (_editorPreviewTimeline == null)
            return;

        _obstacles.Clear();
        foreach (var evt in _editorPreviewTimeline.GetSortedEvents())
        {
            var worldX = SkiLevelTimeline.TimeToWorldX(evt.timeSeconds, playerColumn, _activeRunSpeed);
            SpawnObstacle(worldX, evt.type, evt.lane);
        }
    }

    public void PreviewTimelineAtTime(float levelTimeSeconds)
    {
        if (!_editorPreviewActive || _editorPreviewTimeline == null)
            return;

        _scrollOffset = levelTimeSeconds * _activeRunSpeed;
        ReloadPreviewObstacles();
        RenderFrame();
    }

    public void PreviewTimeline(SkiLevelTimeline timeline, float levelTimeSeconds)
    {
        if (timeline == null)
            return;

        if (!_editorPreviewActive || _editorPreviewTimeline != timeline)
            BeginTimelinePreview(timeline);

        PreviewTimelineAtTime(levelTimeSeconds);
    }

    public void EndTimelinePreview()
    {
        _editorPreviewActive = false;
        _editorPreviewTimeline = null;
        ShowHomeScreen();
    }
}
