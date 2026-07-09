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
    [SerializeField] bool show3DWallPanel;
    [SerializeField] bool showUiOverlay = true;
    [SerializeField] bool showLedGrid = true;
    [SerializeField] float ledGap = 0.14f;
    [SerializeField] RawImage uiPreview;
    [SerializeField] Renderer wallPanel;

    readonly LEDWallBuffer _buffer = new();
    Material _panelMaterial;
    Material _uiMaterial;

    public Texture2D SourceTexture => sourceTexture;
    public Texture2D MatrixTexture => _buffer.Texture;
    public LEDWallBuffer Buffer => _buffer;

    void OnEnable()
    {
        if (ShouldAutoRefresh())
            Refresh();
        else
            UpdatePreviews();
    }

    void OnValidate()
    {
        if (ShouldAutoRefresh())
            Refresh();
        else
            UpdatePreviews();
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
        EnsureDisplayTexture();
        var texture = _buffer.Texture;

        UpdatePanelPreview(texture);
        UpdateUiPreview(texture);
    }

    void EnsureDisplayTexture()
    {
        if (_buffer.Texture != null)
            return;

        _buffer.EnsureTexture();
        _buffer.ClearPixels(Color.black);
        _buffer.Apply();
    }

    void UpdatePanelPreview(Texture2D texture)
    {
        if (wallPanel == null)
            return;

        wallPanel.enabled = show3DWallPanel;
        if (!show3DWallPanel)
            return;

        var material = GetPanelMaterial();
        LEDWallMaterialUtility.ApplyGridSettings(material, showLedGrid, ledGap);
        LEDWallMaterialUtility.ApplyTexture(material, texture);
        wallPanel.sharedMaterial = material;
    }

    void UpdateUiPreview(Texture2D texture)
    {
        if (uiPreview == null)
            return;

        uiPreview.gameObject.SetActive(showUiOverlay);
        if (!showUiOverlay)
            return;

        uiPreview.texture = texture;

        if (!showLedGrid)
        {
            uiPreview.material = null;
            return;
        }

        var material = GetUiMaterial();
        LEDWallMaterialUtility.ApplyGridSettings(material, true, ledGap);
        LEDWallMaterialUtility.ApplyTexture(material, texture);
        uiPreview.material = material;
    }

    Material GetPanelMaterial()
    {
        if (_panelMaterial == null || !LEDWallMaterialUtility.IsLedWallShader(_panelMaterial.shader))
            _panelMaterial = LEDWallMaterialUtility.CreatePanelMaterial();

        return _panelMaterial;
    }

    Material GetUiMaterial()
    {
        if (_uiMaterial == null || !LEDWallMaterialUtility.IsLedWallShader(_uiMaterial.shader))
            _uiMaterial = LEDWallMaterialUtility.CreatePanelMaterial();

        return _uiMaterial;
    }

    void OnDestroy()
    {
        _buffer.Destroy();

        if (_panelMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(_panelMaterial);
            else
                DestroyImmediate(_panelMaterial);
        }

        if (_uiMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(_uiMaterial);
            else
                DestroyImmediate(_uiMaterial);
        }
    }
}
