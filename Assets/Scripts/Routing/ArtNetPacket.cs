using System;
using System.Text;

namespace LedShow.Routing
{
    // Builds the ArtDMX packet: the standard Art-Net message that carries one
    // DMX512 universe (up to 512 channel values) over UDP. Format reference:
    // https://art-net.org.uk/structure/streaming-packets/artdmx-packet-definition/
    public static class ArtNetPacket
    {
        public const int Port = 6454;
        public const int MaxDmxLength = 512;
        public const int HeaderSize = 18;
        public const int MaxPacketSize = HeaderSize + MaxDmxLength;

        private static readonly byte[] ArtNetId = Encoding.ASCII.GetBytes("Art-Net\0");
        private const ushort OpDmx = 0x5000;

        // Writes a full ArtDMX packet into `buffer` (must be at least MaxPacketSize)
        // and returns the number of bytes actually used.
        public static int WriteArtDmx(byte[] buffer, ushort universe, byte sequence, byte[] dmxData, int dmxLength)
        {
            if (dmxLength < 0 || dmxLength > MaxDmxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(dmxLength));
            }

            Array.Copy(ArtNetId, 0, buffer, 0, ArtNetId.Length);

            // OpCode is transmitted low byte first, unlike Length below.
            buffer[8] = (byte)(OpDmx & 0xFF);
            buffer[9] = (byte)((OpDmx >> 8) & 0xFF);

            buffer[10] = 0;  // ProtVerHi
            buffer[11] = 14; // ProtVerLo (Art-Net protocol version 14)

            buffer[12] = sequence;
            buffer[13] = 0; // Physical input port, unused here

            // Port-Address (SubNet+Universe low byte, Net high byte) - little endian.
            buffer[14] = (byte)(universe & 0xFF);
            buffer[15] = (byte)((universe >> 8) & 0xFF);

            // Length is the one field transmitted high byte first (big endian).
            buffer[16] = (byte)((dmxLength >> 8) & 0xFF);
            buffer[17] = (byte)(dmxLength & 0xFF);

            Array.Copy(dmxData, 0, buffer, HeaderSize, dmxLength);

            return HeaderSize + dmxLength;
        }
    }
}
