using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEngine;
using LedShow.Routing;

namespace LedShow.Editor
{
    // Debug tool (P8): listens on the Art-Net UDP port and shows what actually
    // arrives on the network, independently of ArtNetSender's own code - so a
    // bug shared by both wouldn't hide itself. Open via LED Show > ArtNet Monitor.
    public class ArtNetMonitorWindow : EditorWindow
    {
        private UdpClient listener;
        private string lastPacketInfo = "En attente de paquets sur le port UDP 6454...";
        private int packetCount;

        [MenuItem("LED Show/ArtNet Monitor")]
        public static void Open()
        {
            GetWindow<ArtNetMonitorWindow>("ArtNet Monitor");
        }

        private void OnEnable()
        {
            StartListening();
            EditorApplication.update += Poll;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Poll;
            StopListening();
        }

        private void StartListening()
        {
            StopListening();
            try
            {
                listener = new UdpClient(ArtNetPacket.Port);
            }
            catch (SocketException e)
            {
                lastPacketInfo = $"Impossible d'ecouter le port {ArtNetPacket.Port} : {e.Message}";
            }
        }

        private void StopListening()
        {
            listener?.Close();
            listener = null;
        }

        private void Poll()
        {
            if (listener == null || listener.Available <= 0)
            {
                return;
            }

            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (listener.Available > 0)
            {
                byte[] data = listener.Receive(ref remoteEndPoint);
                packetCount++;
                lastPacketInfo = FormatPacket(data, remoteEndPoint);
            }

            Repaint();
        }

        private static string FormatPacket(byte[] data, IPEndPoint from)
        {
            if (data.Length < ArtNetPacket.HeaderSize)
            {
                return $"Paquet invalide ({data.Length} octets) recu de {from}";
            }

            string id = Encoding.ASCII.GetString(data, 0, 7);
            int opCode = data[8] | (data[9] << 8);
            int universe = data[14] | (data[15] << 8);
            int length = (data[16] << 8) | data[17];

            var sb = new StringBuilder();
            sb.AppendLine($"De {from}");
            sb.AppendLine($"ID: {id}   OpCode: 0x{opCode:X4}   Sequence: {data[12]}");
            sb.AppendLine($"Univers: {universe}   Longueur DMX: {length}");
            sb.Append("Premiers canaux: ");
            int channelsToShow = Mathf.Min(16, length);
            for (int i = 0; i < channelsToShow; i++)
            {
                sb.Append(data[ArtNetPacket.HeaderSize + i]).Append(' ');
            }

            return sb.ToString();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Ecoute UDP sur le port 6454 (Art-Net), independamment de l'ArtNetSender. " +
                "Sert a verifier ce qui est reellement envoye sur le reseau, sans materiel physique.",
                MessageType.Info);

            EditorGUILayout.LabelField("Paquets recus", packetCount.ToString());
            EditorGUILayout.Space();
            EditorGUILayout.TextArea(lastPacketInfo, GUILayout.MinHeight(120));

            if (GUILayout.Button("Redemarrer l'ecoute"))
            {
                StartListening();
            }
        }
    }
}
