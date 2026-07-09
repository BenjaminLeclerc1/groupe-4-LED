using UnityEngine;
using LedShow.Core;

namespace LedShow.Routing
{
    // Reads a LedState (from whatever ILedStateSource is plugged in) and routes
    // it to a controller over ArtNet: slices the pixel array into 512-channel
    // universes and sends one ArtDMX packet per universe, at a fixed rate
    // independent of the editor/game frame rate.
    //
    // Knows nothing about how the state was produced (test pattern, render-to-
    // texture...) - only ILedStateSource, per the same decoupling used by the
    // simulator.
    [ExecuteAlways]
    public class LedStateArtNetRouter : MonoBehaviour
    {
        private const int BytesPerPixel = 3; // RGB, one byte per channel

        [Tooltip("Any component implementing ILedStateSource.")]
        [SerializeField] private MonoBehaviour stateSourceBehaviour;

        [Tooltip("Target controller IP. Use 127.0.0.1 while no real controller is configured.")]
        [SerializeField] private string targetIp = "127.0.0.1";

        [SerializeField] private int startUniverse = 0;
        [SerializeField] private float sendRateHz = 40f;

        private ArtNetSender sender;
        private readonly byte[] universeBuffer = new byte[ArtNetPacket.MaxDmxLength];
        private float timeSinceLastSend;
        private string connectedIp;

        private void OnEnable()
        {
            if (stateSourceBehaviour != null && !(stateSourceBehaviour is ILedStateSource))
            {
                Debug.LogError($"{nameof(LedStateArtNetRouter)}: '{stateSourceBehaviour.name}' does not implement ILedStateSource.", this);
            }

            ConnectSender();
        }

        private void OnDisable()
        {
            sender?.Dispose();
            sender = null;
        }

        private void ConnectSender()
        {
            if (sender != null && connectedIp == targetIp)
            {
                return;
            }

            sender?.Dispose();
            sender = new ArtNetSender(targetIp);
            connectedIp = targetIp;
        }

        private void Update()
        {
            // Read the interface fresh every frame rather than caching it in OnEnable:
            // caching it meant a source wired up right after AddComponent (as our
            // "LED Show > Create ..." menus do) could be missed, since OnEnable runs
            // before the field assignment lands.
            var source = stateSourceBehaviour as ILedStateSource;
            if (source == null || source.State == null)
            {
                return;
            }

            ConnectSender();

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
            int pixelIndex = 0;
            ushort universe = (ushort)startUniverse;
            int maxPixelsPerUniverse = ArtNetPacket.MaxDmxLength / BytesPerPixel;

            while (pixelIndex < state.Pixels.Length)
            {
                int offset = 0;
                int pixelsInThisUniverse = Mathf.Min(maxPixelsPerUniverse, state.Pixels.Length - pixelIndex);

                for (int i = 0; i < pixelsInThisUniverse; i++)
                {
                    Color32 pixel = state.Pixels[pixelIndex];
                    universeBuffer[offset] = pixel.r;
                    universeBuffer[offset + 1] = pixel.g;
                    universeBuffer[offset + 2] = pixel.b;
                    offset += BytesPerPixel;
                    pixelIndex++;
                }

                sender.SendUniverse(universe, universeBuffer, offset);
                universe++;
            }
        }
    }
}
