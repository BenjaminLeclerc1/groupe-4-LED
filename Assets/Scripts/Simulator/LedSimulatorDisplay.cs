using UnityEngine;

namespace LedShow.Simulator
{
    // Renders a LedState onto a quad so the LED wall can be previewed inside the
    // Unity editor/Game view without any physical hardware connected.
    [ExecuteAlways]
    [RequireComponent(typeof(LedTestPatternGenerator))]
    public class LedSimulatorDisplay : MonoBehaviour
    {
        private const string QuadName = "LedSimulatorQuad";

        private LedTestPatternGenerator source;
        private Texture2D texture;
        private MeshRenderer quadRenderer;

        private void OnEnable()
        {
            source = GetComponent<LedTestPatternGenerator>();
            BuildQuad();
        }

        private void BuildQuad()
        {
            Transform existing = transform.Find(QuadName);
            GameObject quadObject = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.name = QuadName;
            quadObject.transform.SetParent(transform, false);
            quadObject.hideFlags = HideFlags.DontSave;

            Collider quadCollider = quadObject.GetComponent<Collider>();
            if (quadCollider != null)
            {
                DestroyImmediate(quadCollider);
            }

            quadRenderer = quadObject.GetComponent<MeshRenderer>();
            if (quadRenderer.sharedMaterial == null || quadRenderer.sharedMaterial.shader.name != "Unlit/Texture")
            {
                quadRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
            }
        }

        private void Update()
        {
            var state = source.State;
            if (state == null || quadRenderer == null)
            {
                return;
            }

            if (texture == null || texture.width != state.Width || texture.height != state.Height)
            {
                texture = new Texture2D(state.Width, state.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                };
                quadRenderer.sharedMaterial.mainTexture = texture;
            }

            texture.SetPixels32(state.Pixels);
            texture.Apply(false);
        }
    }
}
