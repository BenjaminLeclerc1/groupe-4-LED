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
    }

    [Header("Sprites")]
    [SerializeField] Texture2D obstacleTexture;

    [Header("Gameplay")]
    [SerializeField] float runSpeed = 34f;
    [SerializeField] float jumpForce = 77f;
    [SerializeField] float gravity = 125f;
    [SerializeField] int playerColumn = 22;
    [SerializeField] float minObstacleSpacing = 42f;
    [SerializeField] float maxObstacleSpacing = 78f;
    [SerializeField] bool runInEditMode;

    LEDWallSimulator _wall;
    SpritePixelData _skier;
    SpritePixelData _obstacle;

    readonly List<Obstacle> _obstacles = new();
    float _scrollOffset;
    float _playerY;
    float _velocityY;
    float _nextSpawnWorldX;
    bool _isRunning;
    bool _isGameOver;
    float _groundY;
    float _jumpImpulse;

    void OnEnable()
    {
        _wall = GetComponent<LEDWallSimulator>();
        StartGame();
    }

    void OnDisable()
    {
        _isRunning = false;
    }

    public void StartGame()
    {
        if (_wall == null)
            _wall = GetComponent<LEDWallSimulator>();

        LoadSprites();

        _scrollOffset = 0f;
        _velocityY = 0f;
        _isGameOver = false;
        _obstacles.Clear();
        _nextSpawnWorldX = 90f;
        _groundY = GetPlayerGroundY();
        _playerY = _groundY;
        var groundDrop = (SkiSlopeRenderer.PlayerGroundTop + 1f) - _groundY;
        _jumpImpulse = jumpForce + groundDrop;
        _isRunning = _skier.HasPixels;
        if (_isRunning && _obstacle.HasPixels)
            SpawnObstacle(_nextSpawnWorldX);

        if (_isRunning)
            RenderFrame();
    }

    public void ForceRender()
    {
        if (_wall == null)
            _wall = GetComponent<LEDWallSimulator>();

        LoadSprites();

        if (!_isRunning && _skier.HasPixels)
            _isRunning = true;

        if (_skier.HasPixels)
            RenderFrame();
    }

    void Update()
    {
        if (!_isRunning)
            return;

        if (!Application.isPlaying)
        {
            if (runInEditMode)
            {
                _scrollOffset += runSpeed * 0.02f;
                RenderFrame();
            }

            return;
        }

        if (_isGameOver)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                StartGame();

            return;
        }

        HandleJumpInput();
        UpdatePhysics();
        UpdateWorld();
        CheckCollisions();
        RenderFrame();
    }

    void HandleJumpInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
            return;

        if (IsGrounded())
            _velocityY = _jumpImpulse;
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
        _scrollOffset += runSpeed * Time.deltaTime;

        while (_obstacle.HasPixels && _nextSpawnWorldX < _scrollOffset + LEDWallConfig.VisibleWidth + 40f)
        {
            _nextSpawnWorldX += Random.Range(minObstacleSpacing, maxObstacleSpacing);
            SpawnObstacle(_nextSpawnWorldX);
        }

        var removeBefore = -(_obstacle.Width + 8);
        for (var i = _obstacles.Count - 1; i >= 0; i--)
        {
            if (_obstacles[i].WorldX - _scrollOffset < removeBefore)
                _obstacles.RemoveAt(i);
        }
    }

    void SpawnObstacle(float worldX)
    {
        _obstacles.Add(new Obstacle { WorldX = worldX });
    }

    void CheckCollisions()
    {
        if (!_skier.HasPixels || !_obstacle.HasPixels)
            return;

        var playerLeft = playerColumn + _skier.HitMinX;
        var playerRight = playerColumn + _skier.HitMaxX;
        var playerBottom = Mathf.FloorToInt(_playerY) + _skier.HitMinY;
        var playerTop = Mathf.FloorToInt(_playerY) + _skier.HitMaxY;

        foreach (var obstacle in _obstacles)
        {
            var screenX = Mathf.FloorToInt(obstacle.WorldX - _scrollOffset);
            var obstacleScreenY = GetObstacleScreenY(screenX);
            var obstacleLeft = screenX + _obstacle.HitMinX;
            var obstacleRight = screenX + _obstacle.HitMaxX;
            var obstacleBottom = obstacleScreenY + _obstacle.HitMinY;
            var obstacleTop = obstacleScreenY + _obstacle.HitMaxY;

            if (playerRight < obstacleLeft || playerLeft > obstacleRight)
                continue;

            if (playerBottom >= obstacleTop - 4)
                continue;

            _isGameOver = true;
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
                applyAfter: true);
        }
        else
        {
            buffer.Apply();
        }

        _wall.UpdatePreviews();
    }

    void DrawObstacles(LEDWallBuffer buffer)
    {
        if (!_obstacle.HasPixels)
            return;

        foreach (var obstacle in _obstacles)
        {
            var screenX = Mathf.FloorToInt(obstacle.WorldX - _scrollOffset);
            if (screenX < -_obstacle.Width || screenX >= LEDWallConfig.VisibleWidth)
                continue;

            var obstacleScreenY = GetObstacleScreenY(screenX);
            buffer.DrawTexture(
                _obstacle.Pixels,
                _obstacle.Width,
                _obstacle.Height,
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

        _skier = SpritePixelData.FromTexture(skierTexture);
        _obstacle = SpritePixelData.FromTexture(rockTexture, stripBlack: false);
    }

    bool IsGrounded()
    {
        return Mathf.Abs(_playerY - _groundY) < 0.5f && Mathf.Abs(_velocityY) < 0.1f;
    }

    int GetGroundTopAtScreen(int screenColumn)
    {
        return SkiSlopeRenderer.GetScreenGroundTop(screenColumn, playerColumn);
    }

    int GetObstacleScreenY(int screenColumn)
    {
        return GetGroundTopAtScreen(screenColumn) - _obstacle.HitMinY;
    }

    float GetPlayerGroundY()
    {
        return GetGroundTopAtScreen(playerColumn) - _skier.HitMinY;
    }
}
