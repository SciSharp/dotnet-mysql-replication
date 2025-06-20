using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;

namespace SciSharp.MySQL.Replication.Protocol
{
    /// <summary>
    /// Base class for MySQL protocol packets.
    /// MySQL protocol packet format: [3 bytes length][1 byte sequence_id][payload]
    /// </summary>
    internal abstract class MySQLPacket
    {
        public byte SequenceId { get; set; }

        /// <summary>
        /// Writes the packet to a stream.
        /// </summary>
        public async Task WriteToStreamAsync(Stream stream)
        {
            var payload = GetPayload();
            var length = payload.Length;

            // Write packet header (3 bytes length + 1 byte sequence_id)
            var header = new byte[4];
            header[0] = (byte)(length & 0xFF);
            header[1] = (byte)((length >> 8) & 0xFF);
            header[2] = (byte)((length >> 16) & 0xFF);
            header[3] = SequenceId;

            await stream.WriteAsync(header).ConfigureAwait(false);
            await stream.WriteAsync(payload).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a packet from a stream.
        /// </summary>
        public static async Task<(byte[] payload, byte sequenceId)> ReadFromStreamAsync(Stream stream)
        {
            // Read packet header
            var header = new byte[4];
            await ReadExactlyAsync(stream, header, 4).ConfigureAwait(false);

            var length = header[0] | (header[1] << 8) | (header[2] << 16);
            var sequenceId = header[3];

            // Read payload
            var payload = new byte[length];
            await ReadExactlyAsync(stream, payload, length).ConfigureAwait(false);

            return (payload, sequenceId);
        }

        /// <summary>
        /// Helper method to read exactly the specified number of bytes.
        /// </summary>
        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int count)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, count - totalBytesRead).ConfigureAwait(false);
                if (bytesRead == 0)
                    throw new EndOfStreamException("Unexpected end of stream while reading MySQL packet");
                totalBytesRead += bytesRead;
            }
        }

        /// <summary>
        /// Gets the payload bytes for this packet.
        /// </summary>
        protected abstract byte[] GetPayload();
    }
}