using System;
using System.Net;
using System.Net.Sockets;

namespace LedShow.Routing
{
    // Opens one UDP socket and sends ArtDMX packets to a single target (a
    // controller's IP). The send buffer is allocated once and reused so
    // sending a universe never allocates garbage on the heap.
    public class ArtNetSender : IDisposable
    {
        private readonly UdpClient client;
        private readonly IPEndPoint endpoint;
        private readonly byte[] sendBuffer = new byte[ArtNetPacket.MaxPacketSize];

        // 0 means "sequencing disabled" in the Art-Net spec, so real sequence
        // numbers wrap from 1 to 255, never touching 0.
        private byte sequence = 1;

        public ArtNetSender(string targetIp, int port = ArtNetPacket.Port)
        {
            client = new UdpClient();
            endpoint = new IPEndPoint(IPAddress.Parse(targetIp), port);
        }

        public void SendUniverse(ushort universe, byte[] dmxData, int dmxLength)
        {
            int packetLength = ArtNetPacket.WriteArtDmx(sendBuffer, universe, sequence, dmxData, dmxLength);
            client.Send(sendBuffer, packetLength, endpoint);
            sequence = sequence == 255 ? (byte)1 : (byte)(sequence + 1);
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
