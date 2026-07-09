using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LEDWallSimulator : MonoBehaviour
{
    [Header("Contenu")]
    [SerializeField] Texture2D sourceTexture;
    [SerializeField] Vector2Int spritePosition = new(-1, -1);
    [SerializeField] bool clearToBlack = true;

    [Header("Preview")]
    [SerializeField] bool showLedGrid = true;
    [SerializeField] float ledGap = 0.14f;
    [SerializeField] RawImage uiPreview;
    [SerializeField] Renderer wallPanel;

    readonly LEDWallBuffer _buffer = new();

    public Texture2D SourceTexture => sourceTexture;
    public Texture2D MatrixTexture => _buffer.Texture;
    public LEDWallBuffer Buffer => _buffer;

    void OnEnable()
    {
        if (ShouldAutoRefresh())
            Refresh();
    }

    void OnValidate()
    {
        if (ShouldAutoRefresh())
            Refresh();
    }

    bool ShouldAutoRefresh()
    {
        if (TryGetComponent<SkiDescentGame>(out var game) && game.isActiveAndEnabled)
            return false;

        return true;
    }

    public void SetUiPreview(RawImage rawImage)
    {
        uiPreview = rawImage;
        UpdatePreviews();
    }

    public void SetWallPanel(Renderer renderer)
    {
        wallPanel = renderer;
        UpdatePreviews();
    }

    public void Refresh()
    {
        if (TryGetComponent<SkiDescentGame>(out var game) && game.isActiveAndEnabled)
        {
            game.ForceRender();
            return;
        }

        if (clearToBlack)
            _buffer.Clear(Color.black);

        if (sourceTexture != null)
        {
            if (spritePosition.x < 0 || spritePosition.y < 0)
                _buffer.DrawTextureCentered(sourceTexture);
            else if (LEDTextureUtility.TryGetPixels(
                sourceTexture,
                out var pixels,
                out var width,
                out var height))
                _buffer.DrawTexture(pixels, width, height, spritePosition);
        }
        else
        {
            _buffer.Apply();
        }

        UpdatePreviews();
    }

    public void SetSourceTexture(Texture2D texture)
    {
        sourceTexture = texture;
        Refresh();
    }

    public void UpdatePreviews()
    {
        if (_buffer.Texture == null)
            return;

        if (uiPreview != null)
            uiPreview.texture = _buffer.Texture;

        if (wallPanel == null)
            return;

        EnsureWallMaterial();
        LEDTextureUtility.ApplyToRenderer(wallPanel, _buffer.Texture);

        var material = wallPanel.sharedMaterial;
        if (material == null)
            return;

        if (material.HasProperty("_GridSize"))
            material.SetFloat("_GridSize", LEDWallConfig.VisibleWidth);

        if (material.HasProperty("_Gap"))
            material.SetFloat("_Gap", showLedGrid ? ledGap : 0f);
    }

    void EnsureWallMaterial()
    {
        var shader = Shader.Find("LED/LEDWallGrid");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (wallPanel.sharedMaterial == null || wallPanel.sharedMaterial.shader != shader)
            wallPanel.sharedMaterial = new Material(shader);
    }

    void OnDestroy()
    {
        _buffer.Destroy();
    }
}
