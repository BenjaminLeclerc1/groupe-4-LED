using UnityEngine;
using LedShow.Core;

namespace LedShow.Routing
{
    // Routes a 128x128 LedState to the 4 real controllers of the GroupeLaps LED
    // wall, using LedWallLayout to translate each pixel into the exact
    // (controller, universe, channel) address dictated by its physical wiring.
    [ExecuteAlways]
    public class LedWallArtNetRouter : MonoBehaviour
    {
        [Tooltip("Any component implementing ILedStateSource. Only the first 128x128 pixels are used.")]
        [SerializeField] private MonoBehaviour stateSourceBehaviour;

        [Tooltip("IP des 4 controleurs. Laisse vide pour utiliser LedWallLayout par defaut.")]
        [SerializeField] private string[] controllerIps =
        {
            "192.168.1.45",
            "192.168.1.46",
            "192.168.1.47",
            "192.168.1.48",
        };

        [SerializeField] private float sendRateHz = 40f;
        [Tooltip("Intensite globale du mur LED (0 = eteint, 1 = plein).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float ledBrightness = 0.35f;
        [Tooltip("Assombrit encore plus les couleurs peu saturees (ciel / neige) pour faire ressortir les sprites.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float backgroundDim = 0.55f;
        [SerializeField] private bool logSendStatus = true;

        private ArtNetSender[] senders;
        private byte[][][] universeBuffers;
        private float timeSinceLastSend;
        private bool loggedMissingSource;
        private int framesSent;

        public void SetStateSource(MonoBehaviour source)
        {
            stateSourceBehaviour = source;
        }

        private string[] ResolveControllerIps()
        {
            if (controllerIps != null && controllerIps.Length == LedWallLayout.ControllerIps.Length)
            {
                var valid = true;
                for (int i = 0; i < controllerIps.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(controllerIps[i]))
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                    return controllerIps;
            }

            return LedWallLayout.ControllerIps;
        }

        private void OnEnable()
        {
            if (stateSourceBehaviour != null && !(stateSourceBehaviour is ILedStateSource))
            {
                Debug.LogError($"{nameof(LedWallArtNetRouter)}: '{stateSourceBehaviour.name}' does not implement ILedStateSource.", this);
            }

            var ips = ResolveControllerIps();
            senders = new ArtNetSender[ips.Length];
            universeBuffers = new byte[ips.Length][][];
            for (int c = 0; c < senders.Length; c++)
            {
                senders[c] = new ArtNetSender(ips[c]);
                universeBuffers[c] = new byte[LedWallLayout.UniversesPerController][];
                for (int u = 0; u < LedWallLayout.UniversesPerController; u++)
                {
                    universeBuffers[c][u] = new byte[ArtNetPacket.MaxDmxLength];
                }
            }

            framesSent = 0;
            loggedMissingSource = false;

            if (logSendStatus)
            {
                Debug.Log($"{nameof(LedWallArtNetRouter)}: envoi Art-Net vers {string.Join(", ", ips)} (UDP 6454).", this);
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
                if (logSendStatus && !loggedMissingSource && Application.isPlaying)
                {
                    loggedMissingSource = true;
                    Debug.LogWarning(
                        $"{nameof(LedWallArtNetRouter)}: aucune source branchee (State Source Behaviour). " +
                        "Utilise LED Show > Connect Ski Game To Real Wall.",
                        this);
                }

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

            framesSent++;
            if (logSendStatus && framesSent == 1)
            {
                Debug.Log($"{nameof(LedWallArtNetRouter)}: premier frame Art-Net envoye ({source.State.Width}x{source.State.Height}).", this);
            }
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
                    Color32 pixel = ScalePixelForLed(state.Get(x, y));
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

        Color32 ScalePixelForLed(Color32 pixel)
        {
            float maxC = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) / 255f;
            float minC = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b)) / 255f;
            float saturation = maxC > 0.001f ? (maxC - minC) / maxC : 0f;

            // Fond peu sature (ciel/neige) plus sombre ; sprites colores plus visibles.
            float factor = ledBrightness * Mathf.Lerp(backgroundDim, 1f, saturation);
            return new Color32(
                ScaleByte(pixel.r, factor),
                ScaleByte(pixel.g, factor),
                ScaleByte(pixel.b, factor),
                pixel.a);
        }

        static byte ScaleByte(byte value, float factor)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * factor), 0, 255);
        }
    }
}
