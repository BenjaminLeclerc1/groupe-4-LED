using UnityEngine;
using LedShow.Core;

namespace LedShow.Authoring
{
    // Bridges a teammate's LEDWallSimulator (ski game, sprite drawing, etc.)
    // into our LedState pipeline: reads its MatrixTexture every frame so the
    // same content can flow through our LedSimulatorDisplay and
    // LedWallArtNetRouter, without either system needing to know the other
    // exists.
    [ExecuteAlways]
    public class LedWallSimulatorBridgeSource : MonoBehaviour, ILedStateSource
    {
        [SerializeField] private LEDWallSimulator wallSimulator;

        public LedState State { get; private set; }

        private void Update()
        {
            if (wallSimulator == null)
            {
                return;
            }

            Texture2D matrixTexture = wallSimulator.MatrixTexture;
            if (matrixTexture == null)
            {
                return;
            }

            int width = matrixTexture.width;
            int height = matrixTexture.height;

            if (State == null || State.Width != width || State.Height != height)
            {
                State = new LedState(width, height);
            }

            // Unlike LedRenderToTextureSource (which reads back a camera-rendered
            // RenderTexture and needs a flip to correct a DX11-specific quirk),
            // this texture is populated directly on the CPU via SetPixels32, so
            // GetPixels32 here already matches LedState's row order - no flip.
            Color32[] pixels = matrixTexture.GetPixels32();
            System.Array.Copy(pixels, State.Pixels, pixels.Length);
        }
    }
}
