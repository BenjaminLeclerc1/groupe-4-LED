using System;
using UnityEngine;
using UnityEngine.UI;

public struct LEDWallBuildResult
{
    public GameObject WallRoot;
    public LEDWallSimulator Simulator;
    public Renderer PanelRenderer;
    public RawImage UiPreview;
}

public struct LEDWallBuildOptions
{
    public bool ReuseExisting;
    public Action<GameObject, string> OnCreated;
    public Action<UnityEngine.Object> OnDestroy;

    public static LEDWallBuildOptions Runtime => new()
    {
        ReuseExisting = false,
        OnDestroy = obj =>
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }
    };

#if UNITY_EDITOR
    public static LEDWallBuildOptions Editor => new()
    {
        ReuseExisting = true,
        OnCreated = (obj, label) => UnityEditor.Undo.RegisterCreatedObjectUndo(obj, label),
        OnDestroy = obj =>
        {
            if (obj != null)
                UnityEditor.Undo.DestroyObjectImmediate(obj);
        }
    };
#endif
}

public static class LEDWallSceneBuilder
{
    public const float FrameThickness = 0.08f;
    public const int UiPreviewPixelSize = 5;

    public static LEDWallBuildResult EnsureWall(LEDWallBuildOptions options, Texture2D sourceTexture = null)
    {
        var wallRoot = EnsureWallRoot(options, out var simulator);
        var panelRenderer = EnsurePanel(wallRoot.transform, options);
        EnsureFrame(wallRoot.transform, options);
        var uiPreview = EnsureUiPreview(options);
        PositionMainCamera();

        simulator.SetWallPanel(panelRenderer);
        simulator.SetUiPreview(uiPreview);

        if (sourceTexture != null)
            simulator.SetSourceTexture(sourceTexture);
        else if (!simulator.TryGetComponent<SkiDescentGame>(out _))
            simulator.Refresh();

        return new LEDWallBuildResult
        {
            WallRoot = wallRoot,
            Simulator = simulator,
            PanelRenderer = panelRenderer,
            UiPreview = uiPreview
        };
    }

    public static GameObject EnsureWallRoot(LEDWallBuildOptions options, out LEDWallSimulator simulator)
    {
        GameObject wallRoot = null;

        if (options.ReuseExisting)
        {
            wallRoot = GameObject.Find("LED Wall");
            if (wallRoot == null)
            {
                var legacy = GameObject.Find("LED Matrix");
                if (legacy != null)
                {
                    legacy.name = "LED Wall";
                    wallRoot = legacy;
                }
            }
        }

        if (wallRoot == null)
        {
            wallRoot = new GameObject("LED Wall");
            options.OnCreated?.Invoke(wallRoot, "Create LED Wall");
        }

        wallRoot.name = "LED Wall";
        wallRoot.transform.position = Vector3.zero;

        simulator = wallRoot.GetComponent<LEDWallSimulator>();
        if (simulator == null)
        {
#if UNITY_EDITOR
            if (options.OnCreated != null)
                simulator = UnityEditor.Undo.AddComponent<LEDWallSimulator>(wallRoot);
            else
#endif
                simulator = wallRoot.AddComponent<LEDWallSimulator>();
        }

        if (wallRoot.GetComponent<SkiDescentGame>() == null)
        {
#if UNITY_EDITOR
            if (options.OnCreated != null)
                UnityEditor.Undo.AddComponent<SkiDescentGame>(wallRoot);
            else
#endif
                wallRoot.AddComponent<SkiDescentGame>();
        }

        return wallRoot;
    }

    public static Renderer EnsurePanel(Transform parent, LEDWallBuildOptions options)
    {
        Transform panelTransform = null;

        if (options.ReuseExisting)
        {
            panelTransform = parent.Find("LED Panel");
            if (panelTransform == null)
            {
                var legacy = parent.Find("LED Screen Preview");
                if (legacy != null)
                {
                    legacy.name = "LED Panel";
                    panelTransform = legacy;
                }
            }
        }

        GameObject panelObject;
        if (panelTransform == null)
        {
            panelObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            options.OnCreated?.Invoke(panelObject, "Create LED Panel");
            panelObject.name = "LED Panel";
            panelObject.transform.SetParent(parent, false);

            var collider = panelObject.GetComponent<Collider>();
            if (collider != null)
                options.OnDestroy?.Invoke(collider);
        }
        else
        {
            panelObject = panelTransform.gameObject;
        }

        var size = LEDWallConfig.PhysicalSizeMeters;
        panelObject.transform.localPosition = new Vector3(0f, size * 0.5f, 0f);
        panelObject.transform.localRotation = Quaternion.identity;
        panelObject.transform.localScale = new Vector3(size, size, 1f);

        var renderer = panelObject.GetComponent<MeshRenderer>();
        var shader = Shader.Find("LED/LEDWallGrid");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader != shader)
            renderer.sharedMaterial = new Material(shader);

        return renderer;
    }

    public static void EnsureFrame(Transform parent, LEDWallBuildOptions options)
    {
        Transform frameRoot = null;

        if (options.ReuseExisting)
            frameRoot = parent.Find("Frame");

        if (frameRoot == null)
        {
            var frameObject = new GameObject("Frame");
            options.OnCreated?.Invoke(frameObject, "Create LED Frame");
            frameObject.transform.SetParent(parent, false);
            frameRoot = frameObject.transform;
        }

        var size = LEDWallConfig.PhysicalSizeMeters;
        var centerY = size * 0.5f;
        var half = size * 0.5f;
        var thickness = FrameThickness;
        var depth = 0.05f;

        EnsureFrameBar(frameRoot, "Top", new Vector3(0f, centerY + half + thickness * 0.5f, -depth), new Vector3(size + thickness * 2f, thickness, depth), options);
        EnsureFrameBar(frameRoot, "Bottom", new Vector3(0f, centerY - half - thickness * 0.5f, -depth), new Vector3(size + thickness * 2f, thickness, depth), options);
        EnsureFrameBar(frameRoot, "Left", new Vector3(-half - thickness * 0.5f, centerY, -depth), new Vector3(thickness, size, depth), options);
        EnsureFrameBar(frameRoot, "Right", new Vector3(half + thickness * 0.5f, centerY, -depth), new Vector3(thickness, size, depth), options);
    }

    static void EnsureFrameBar(Transform parent, string name, Vector3 localPosition, Vector3 localScale, LEDWallBuildOptions options)
    {
        var barTransform = options.ReuseExisting ? parent.Find(name) : null;
        GameObject barObject;

        if (barTransform == null)
        {
            barObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            options.OnCreated?.Invoke(barObject, "Create LED Frame Bar");
            barObject.name = name;
            barObject.transform.SetParent(parent, false);

            var collider = barObject.GetComponent<Collider>();
            if (collider != null)
                options.OnDestroy?.Invoke(collider);
        }
        else
        {
            barObject = barTransform.gameObject;
        }

        barObject.transform.localPosition = localPosition;
        barObject.transform.localRotation = Quaternion.identity;
        barObject.transform.localScale = localScale;

        var renderer = barObject.GetComponent<MeshRenderer>();
        if (renderer.sharedMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            renderer.sharedMaterial = new Material(shader);
        }

        renderer.sharedMaterial.color = new Color(0.12f, 0.12f, 0.12f);
    }

    public static RawImage EnsureUiPreview(LEDWallBuildOptions options)
    {
        Canvas canvas = null;

        if (options.ReuseExisting)
            canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            var canvasObject = new GameObject("LED Preview Canvas");
            options.OnCreated?.Invoke(canvasObject, "Create LED Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        var previewTransform = options.ReuseExisting ? canvas.transform.Find("LED Preview") : null;
        GameObject previewObject;

        if (previewTransform == null)
        {
            previewObject = new GameObject("LED Preview", typeof(RectTransform));
            options.OnCreated?.Invoke(previewObject, "Create LED Preview");
            previewObject.transform.SetParent(canvas.transform, false);
        }
        else
        {
            previewObject = previewTransform.gameObject;
        }

        var rectTransform = previewObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0.5f);
        rectTransform.anchorMax = new Vector2(1f, 0.5f);
        rectTransform.pivot = new Vector2(1f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(-24f, 0f);
        rectTransform.sizeDelta = new Vector2(
            LEDWallConfig.VisibleWidth * UiPreviewPixelSize,
            LEDWallConfig.VisibleHeight * UiPreviewPixelSize);

        var rawImage = previewObject.GetComponent<RawImage>();
        if (rawImage == null)
            rawImage = previewObject.AddComponent<RawImage>();

        rawImage.raycastTarget = false;
        rawImage.color = Color.white;
        return rawImage;
    }

    public static void PositionMainCamera()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        camera.transform.position = new Vector3(0f, 1f, -3.8f);
        camera.transform.rotation = Quaternion.identity;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
    }
}
