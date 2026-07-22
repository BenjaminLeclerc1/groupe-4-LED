using UnityEngine;
using LedShow.Core;

namespace LedShow.Authoring
{
    // Bridges LEDWallSimulator (ski game) into the LedState pipeline so the
    // same content can flow to LedWallArtNetRouter. Play-only.
    [DefaultExecutionOrder(-100)]
    public class LedWallSimulatorBridgeSource : MonoBehaviour, ILedStateSource
    {
        [SerializeField] private LEDWallSimulator wallSimulator;

        public LedState State { get; private set; }

        public void SetWallSimulator(LEDWallSimulator simulator)
        {
            wallSimulator = simulator;
        }

        private void Update()
        {
            if (!Application.isPlaying || wallSimulator == null)
                return;

            Texture2D matrixTexture = wallSimulator.MatrixTexture;
            if (matrixTexture == null)
                return;

            int width = matrixTexture.width;
            int height = matrixTexture.height;

            if (State == null || State.Width != width || State.Height != height)
                State = new LedState(width, height);

            var pixels = matrixTexture.GetPixels32();
            if (pixels == null || pixels.Length != State.Pixels.Length)
                return;

            System.Array.Copy(pixels, State.Pixels, pixels.Length);
        }
    }
}
