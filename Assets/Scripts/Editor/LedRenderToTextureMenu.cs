using UnityEditor;
using UnityEngine;
using LedShow.Authoring;
using LedShow.Simulator;

namespace LedShow.Editor
{
    public static class LedRenderToTextureMenu
    {
        [MenuItem("LED Show/Create Render-To-Texture Demo")]
        public static void CreateDemo()
        {
            var root = new GameObject("LED Render-To-Texture Demo");

            var cameraObject = new GameObject("Capture Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            // Left enabled on purpose: with targetTexture set (below, via
            // LedRenderToTextureSource), Unity renders this camera into that
            // texture automatically every frame - it never touches the screen.

            CreateUnlitShape(PrimitiveType.Cube, root.transform, new Vector3(-2f, 0f, 0f), Color.red, addSpinner: false, localScale: Vector3.one);
            // Flattened on Z: spinning around Y makes its apparent width clearly shrink/grow,
            // which stays visible even with flat unlit shading (a plain rotating cube would not).
            CreateUnlitShape(PrimitiveType.Cube, root.transform, Vector3.zero, Color.green, addSpinner: true, localScale: new Vector3(1.5f, 1.5f, 0.2f));
            CreateUnlitShape(PrimitiveType.Sphere, root.transform, new Vector3(2f, 0f, 0f), Color.blue, addSpinner: false, localScale: Vector3.one);

            var source = root.AddComponent<LedRenderToTextureSource>();
            var serializedSource = new SerializedObject(source);
            serializedSource.FindProperty("sourceCamera").objectReferenceValue = camera;
            serializedSource.ApplyModifiedProperties();

            var existingDisplay = Object.FindAnyObjectByType<LedSimulatorDisplay>();
            if (existingDisplay != null)
            {
                var serializedDisplay = new SerializedObject(existingDisplay);
                serializedDisplay.FindProperty("stateSourceBehaviour").objectReferenceValue = source;
                serializedDisplay.ApplyModifiedProperties();
                Debug.Log("LED Simulator existant trouve : bascule sur LedRenderToTextureSource comme source.", existingDisplay);
            }
            else
            {
                Debug.Log("Aucun LED Simulator dans la scene : cree-en un via 'LED Show > Create LED Simulator' " +
                          "puis glisse cet objet dans son champ 'State Source Behaviour'.", source);
            }

            Selection.activeGameObject = root;
        }

        private static void CreateUnlitShape(PrimitiveType type, Transform parent, Vector3 localPosition, Color color, bool addSpinner, Vector3 localScale)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = localScale;

            var renderer = shape.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color);
            renderer.sharedMaterial = material;

            if (addSpinner)
            {
                shape.AddComponent<DemoSpinner>();
            }
        }
    }
}
