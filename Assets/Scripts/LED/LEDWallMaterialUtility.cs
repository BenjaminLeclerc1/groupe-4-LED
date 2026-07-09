using UnityEngine;

public static class LEDWallMaterialUtility
{
    static readonly int GridSizeId = Shader.PropertyToID("_GridSize");
    static readonly int GapId = Shader.PropertyToID("_Gap");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    public static Shader GetLedWallShader()
    {
        var shader = Shader.Find("LED/LEDWallGrid");
        if (shader != null)
            return shader;

        return Shader.Find("Universal Render Pipeline/Unlit");
    }

    public static bool IsLedWallShader(Shader shader)
    {
        return shader != null && shader.name == "LED/LEDWallGrid";
    }

    public static Material CreatePanelMaterial()
    {
        var material = new Material(GetLedWallShader());
        ApplyGridSettings(material, showGrid: true, gap: 0.14f);
        return material;
    }

    public static void ApplyGridSettings(Material material, bool showGrid, float gap)
    {
        if (material == null || !IsLedWallShader(material.shader))
            return;

        material.SetFloat(GridSizeId, LEDWallConfig.VisibleWidth);
        material.SetFloat(GapId, showGrid ? gap : 0f);
    }

    public static void ApplyTexture(Material material, Texture texture)
    {
        if (material == null || texture == null)
            return;

        if (material.HasProperty(BaseMapId))
            material.SetTexture(BaseMapId, texture);
        else
            material.mainTexture = texture;
    }
}
