#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LEDAssetUtility
{
    public const string SkieurPath = "Assets/Sprite/skieur.png";
    public const string ObstaclePath = "Assets/Sprite/roche_obstacle.png";

    public static Texture2D LoadSkieurTexture() => LoadTexture(SkieurPath);

    public static Texture2D LoadObstacleTexture() => LoadTexture(ObstaclePath);

    public static Texture2D LoadTexture(string assetPath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            return sprite.texture;

        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Texture2D>()
            .FirstOrDefault();
    }

    public static void ConfigureTextureImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    public static void ConfigureAllSpriteImports()
    {
        ConfigureTextureImport(SkieurPath);
        ConfigureTextureImport(ObstaclePath);
    }
}
#endif
