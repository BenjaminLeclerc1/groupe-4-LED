#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LEDMatrixSetupMenu
{
    static bool _isSettingUp;

    [MenuItem("LED/Configurer la scène (mur LED 2m)")]
    public static void SetupScene()
    {
        SetupSceneFromPreview(openPreviewWindow: true);
    }

    public static void SetupSceneFromPreview(bool openPreviewWindow = true)
    {
        if (_isSettingUp)
            return;

        _isSettingUp = true;
        try
        {
            SetupSceneInternal(openPreviewWindow);
        }
        finally
        {
            _isSettingUp = false;
        }
    }

    static void SetupSceneInternal(bool openPreviewWindow)
    {
        LEDAssetUtility.ConfigureAllSpriteImports();

        var sprite = LEDAssetUtility.LoadSkieurTexture();
        if (sprite == null)
        {
            EditorUtility.DisplayDialog(
                "Sprite introuvable",
                $"Impossible de charger {LEDAssetUtility.SkieurPath}.\nUtilise LED > Convertir les sprites HTML en PNG.",
                "OK");
            return;
        }

        var result = LEDWallSceneBuilder.EnsureWall(LEDWallBuildOptions.Editor, sprite);
        ApplySimulatorSettings(result.Simulator, sprite, result.UiPreview, result.PanelRenderer, result.WallRoot);

        EditorSceneManager.MarkSceneDirty(result.WallRoot.scene);
        Selection.activeGameObject = result.WallRoot;
        SceneView.RepaintAll();

        if (openPreviewWindow)
            LEDPreviewWindow.Open();
    }

    [MenuItem("LED/Convertir les sprites HTML en PNG")]
    static void ConvertHtmlToPng()
    {
        var projectRoot = Application.dataPath.Replace("\\", "/").Replace("/Assets", "");
        var scriptPath = $"{projectRoot}/tools/convert_pixel_art.py";

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "py",
                Arguments = $"-3 \"{scriptPath}\" all",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        AssetDatabase.Refresh();

        if (process.ExitCode != 0)
        {
            EditorUtility.DisplayDialog("Erreur conversion", error, "OK");
            return;
        }

        LEDAssetUtility.ConfigureAllSpriteImports();
        EditorUtility.DisplayDialog("Conversion terminée", output.Trim(), "OK");
    }

    static void ApplySimulatorSettings(
        LEDWallSimulator simulator,
        Texture2D sprite,
        RawImage rawImage,
        Renderer panelRenderer,
        GameObject wallRoot)
    {
        var serializedSimulator = new SerializedObject(simulator);
        serializedSimulator.FindProperty("sourceTexture").objectReferenceValue = sprite;
        serializedSimulator.FindProperty("uiPreview").objectReferenceValue = rawImage;
        serializedSimulator.FindProperty("wallPanel").objectReferenceValue = panelRenderer;
        serializedSimulator.FindProperty("show3DWallPanel").boolValue = false;
        serializedSimulator.FindProperty("showUiOverlay").boolValue = true;
        serializedSimulator.FindProperty("clearToBlack").boolValue = true;
        serializedSimulator.FindProperty("showLedGrid").boolValue = true;
        serializedSimulator.ApplyModifiedPropertiesWithoutUndo();

        simulator.SetUiPreview(rawImage);
        simulator.SetWallPanel(panelRenderer);
        simulator.SetSourceTexture(sprite);

        var game = wallRoot.GetComponent<SkiDescentGame>();
        if (game == null)
            return;

        var obstacle = LEDAssetUtility.LoadObstacleTexture();
        var serializedGame = new SerializedObject(game);
        serializedGame.FindProperty("obstacleTexture").objectReferenceValue = obstacle;
        serializedGame.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
