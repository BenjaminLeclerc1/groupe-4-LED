using UnityEngine;
using LedShow.Core;

namespace LedShow.Routing
{
    // Routes a 128x128 LedState to the 4 real controllers of the GroupeLaps LED
    // wall, using LedWallLayout to translate each pixel into the exact
    // (controller, universe, channel) address dictated by its physical wiring.
    //
    // Unlike LedStateArtNetRouter (one IP, naive sequential mapping), this one
    // is specific to this wall's real topology - kept as a separate class so
    // the simple single-target router still exists for generic testing.
    [ExecuteAlways]
    public class LedWallArtNetRouter : MonoBehaviour
    {
        [Tooltip("Any component implementing ILedStateSource. Only the first 128x128 pixels are used.")]
        [SerializeField] private MonoBehaviour stateSourceBehaviour;

        [SerializeField] private float sendRateHz = 40f;

        private ArtNetSender[] senders;
        private byte[][][] universeBuffers; // [controller][localUniverse] -> 512 DMX bytes
        private float timeSinceLastSend;

        private void OnEnable()
        {
            if (stateSourceBehaviour != null && !(stateSourceBehaviour is ILedStateSource))
            {
                Debug.LogError($"{nameof(LedWallArtNetRouter)}: '{stateSourceBehaviour.name}' does not implement ILedStateSource.", this);
            }

            senders = new ArtNetSender[LedWallLayout.ControllerIps.Length];
            universeBuffers = new byte[LedWallLayout.ControllerIps.Length][][];
            for (int c = 0; c < senders.Length; c++)
            {
                senders[c] = new ArtNetSender(LedWallLayout.ControllerIps[c]);
                universeBuffers[c] = new byte[LedWallLayout.UniversesPerController][];
                for (int u = 0; u < LedWallLayout.UniversesPerController; u++)
                {
                    universeBuffers[c][u] = new byte[ArtNetPacket.MaxDmxLength];
                }
            }
        }

        private void OnDisable()
        {
            if (senders != null)
            {
                foreach (ArtNetSender sender in senders)
                {
                    sender?.Dispose();
                }
            }

            senders = null;
            universeBuffers = null;
        }

        private void Update()
        {
            var source = stateSourceBehaviour as ILedStateSource;
            if (source == null || source.State == null || senders == null)
            {
                return;
            }

            timeSinceLastSend += Time.unscaledDeltaTime;
            float interval = 1f / Mathf.Max(1f, sendRateHz);
            if (timeSinceLastSend < interval)
            {
                return;
            }

            timeSinceLastSend = 0f;
            SendState(source.State);
        }

        private void SendState(LedState state)
        {
            int width = Mathf.Min(state.Width, LedWallLayout.Width);
            int height = Mathf.Min(state.Height, LedWallLayout.Height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    LedWallLayout.LedAddress address = LedWallLayout.GetAddress(x, y);
                    Color32 pixel = state.Get(x, y);
                    byte[] buffer = universeBuffers[address.ControllerIndex][address.LocalUniverse];
                    buffer[address.ChannelOffset] = pixel.r;
                    buffer[address.ChannelOffset + 1] = pixel.g;
                    buffer[address.ChannelOffset + 2] = pixel.b;
                }
            }

            for (int c = 0; c < senders.Length; c++)
            {
                for (int u = 0; u < LedWallLayout.UniversesPerController; u++)
                {
                    senders[c].SendUniverse((ushort)u, universeBuffers[c][u], ArtNetPacket.MaxDmxLength);
                }
            }
        }
    }
}
