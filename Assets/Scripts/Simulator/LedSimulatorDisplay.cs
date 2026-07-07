using UnityEngine;
using LedShow.Core;

namespace LedShow.Simulator
{
    // Renders a LedState onto a quad so the LED wall can be previewed inside the
    // Unity editor/Game view without any physical hardware connected.
    //
    // Depends only on ILedStateSource, not on LedTestPatternGenerator directly:
    // any producer (fake patterns today, render-to-texture tomorrow) can be
    // plugged in here without touching this class.
    [ExecuteAlways]
    public class LedSimulatorDisplay : MonoBehaviour
    {
        [Tooltip("Any component implementing ILedStateSource (e.g. LedTestPatternGenerator, LedRenderToTextureSource).")]
        [SerializeField] private MonoBehaviour stateSourceBehaviour;

        private const string QuadName = "LedSimulatorQuad";

        private ILedStateSource source;
        private Texture2D texture;
        private MeshRenderer quadRenderer;

        private void OnEnable()
        {
            source = stateSourceBehaviour as ILedStateSource;
            if (stateSourceBehaviour != null && source == null)
            {
                Debug.LogError($"{nameof(LedSimulatorDisplay)}: '{stateSourceBehaviour.name}' does not implement ILedStateSource.", this);
            }

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
            if (source == null || quadRenderer == null)
            {
                return;
            }

            var state = source.State;
            if (state == null)
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
