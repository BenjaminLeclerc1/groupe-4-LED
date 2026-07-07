using UnityEngine;
using LedShow.Core;

namespace LedShow.Authoring
{
    // Turns whatever a Camera sees into a LedState: the camera renders into a
    // low-resolution RenderTexture (one pixel per LED), which is read back into
    // a Color32 buffer. Any Unity content (video, particles, animated shapes,
    // shaders...) becomes usable as show content without this class knowing
    // anything about it - it only ever deals with pixels.
    [ExecuteAlways]
    public class LedRenderToTextureSource : MonoBehaviour, ILedStateSource
    {
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private int width = 32;
        [SerializeField] private int height = 18;

        private RenderTexture renderTexture;
        private Texture2D readbackTexture;

        public LedState State { get; private set; }

        private void OnEnable()
        {
            AllocateResources();
        }

        private void OnDisable()
        {
            ReleaseResources();
        }

        private void AllocateResources()
        {
            if (renderTexture != null && renderTexture.width == width && renderTexture.height == height)
            {
                return;
            }

            ReleaseResources();

            renderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
            };
            readbackTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            State = new LedState(width, height);

            if (sourceCamera != null)
            {
                sourceCamera.targetTexture = renderTexture;
            }
        }

        private void ReleaseResources()
        {
            if (sourceCamera != null && sourceCamera.targetTexture == renderTexture)
            {
                sourceCamera.targetTexture = null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
            }
        }

        private void LateUpdate()
        {
            if (sourceCamera == null)
            {
                return;
            }

            AllocateResources();

            // In edit mode nothing else asks the camera to render, so we drive it ourselves.
            sourceCamera.Render();

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            readbackTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readbackTexture.Apply(false);
            RenderTexture.active = previousActive;

            // Texture rows are bottom-to-top; LedState rows are top-to-bottom (matches
            // how the simulator/screen are addressed), so we flip while copying.
            Color32[] readbackPixels = readbackTexture.GetPixels32();
            for (int y = 0; y < height; y++)
            {
                int sourceRow = height - 1 - y;
                System.Array.Copy(readbackPixels, sourceRow * width, State.Pixels, y * width, width);
            }
        }
    }
}
