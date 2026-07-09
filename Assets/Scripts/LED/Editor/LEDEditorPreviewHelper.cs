#if UNITY_EDITOR
using UnityEngine;

public static class LEDEditorPreviewHelper
{
    public static Texture2D CaptureSimulatorPreview(LEDWallSimulator simulator)
    {
        if (simulator == null)
            return BuildFallbackSkieurPreview();

        simulator.Refresh();

        if (simulator.MatrixTexture != null)
        {
            var copy = Object.Instantiate(simulator.MatrixTexture);
            copy.hideFlags = HideFlags.HideAndDontSave;
            return copy;
        }

        return LEDMatrixPreviewUtility.BuildMatrixTexture(simulator.SourceTexture);
    }

    public static Texture2D BuildFallbackSkieurPreview()
    {
        LEDAssetUtility.ConfigureAllSpriteImports();
        return LEDMatrixPreviewUtility.BuildMatrixTexture(LEDAssetUtility.LoadSkieurTexture());
    }
}
#endif
