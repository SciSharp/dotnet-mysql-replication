using System;
using System.Text;

namespace SciSharp.MySQL.Replication.Protocol
{
    /// <summary>
    /// Represents the initial handshake packet sent by the MySQL server.
    /// Reference: https://dev.mysql.com/doc/internals/en/connection-phase-packets.html#packet-Protocol::Handshake
    /// </summary>
    internal class HandshakePacket
    {
        public byte ProtocolVersion { get; set; }
        public string ServerVersion { get; set; }
        public uint ConnectionId { get; set; }
        public byte[] AuthPluginDataPart1 { get; set; } // 8 bytes
        public uint CapabilityFlagsLower { get; set; } // lower 16 bits
        public byte CharacterSet { get; set; }
        public ushort StatusFlags { get; set; }
        public uint CapabilityFlagsUpper { get; set; } // upper 16 bits
        public byte AuthPluginDataLength { get; set; }
        public byte[] AuthPluginDataPart2 { get; set; } // max 13 bytes (total auth_plugin_data is 21 bytes)
        public string AuthPluginName { get; set; }

        /// <summary>
        /// Gets the combined capability flags (lower 16 bits + upper 16 bits).
        /// </summary>
        public uint CapabilityFlags => CapabilityFlagsLower | (CapabilityFlagsUpper << 16);

        /// <summary>
        /// Gets the complete authentication plugin data (part1 + part2).
        /// </summary>
        public byte[] AuthPluginData
        {
            get
            {
                var result = new byte[20]; // MySQL uses 20 bytes for auth data
                Array.Copy(AuthPluginDataPart1, 0, result, 0, 8);
                if (AuthPluginDataPart2 != null)
                {
                    var copyLength = Math.Min(AuthPluginDataPart2.Length, 12);
                    Array.Copy(AuthPluginDataPart2, 0, result, 8, copyLength);
                }
                return result;
            }
        }

        /// <summary>
        /// Parses a handshake packet from raw bytes.
        /// </summary>
        public static HandshakePacket Parse(byte[] payload)
        {
            var packet = new HandshakePacket();
            int offset = 0;

            // Protocol version (1 byte)
            packet.ProtocolVersion = payload[offset++];

            // Server version (null-terminated string)
            var serverVersionEnd = Array.IndexOf(payload, (byte)0, offset);
            packet.ServerVersion = Encoding.UTF8.GetString(payload, offset, serverVersionEnd - offset);
            offset = serverVersionEnd + 1;

            // Connection ID (4 bytes)
            packet.ConnectionId = BitConverter.ToUInt32(payload, offset);
            offset += 4;

            // Auth plugin data part 1 (8 bytes)
            packet.AuthPluginDataPart1 = new byte[8];
            Array.Copy(payload, offset, packet.AuthPluginDataPart1, 0, 8);
            offset += 8;

            // Filter (1 byte) - always 0x00
            offset++;

            // Capability flags lower 16 bits (2 bytes)
            packet.CapabilityFlagsLower = BitConverter.ToUInt16(payload, offset);
            offset += 2;

            // Character set (1 byte)
            packet.CharacterSet = payload[offset++];

            // Status flags (2 bytes)
            packet.StatusFlags = BitConverter.ToUInt16(payload, offset);
            offset += 2;

            // Capability flags upper 16 bits (2 bytes)
            packet.CapabilityFlagsUpper = BitConverter.ToUInt16(payload, offset);
            offset += 2;

            // Auth plugin data length (1 byte)
            packet.AuthPluginDataLength = payload[offset++];

            // Reserved (10 bytes) - skip
            offset += 10;

            // Auth plugin data part 2 (max 13 bytes, but actual length is auth_plugin_data_len - 8)
            if (packet.AuthPluginDataLength > 8)
            {
                var part2Length = Math.Min(packet.AuthPluginDataLength - 8, 13);
                packet.AuthPluginDataPart2 = new byte[part2Length];
                Array.Copy(payload, offset, packet.AuthPluginDataPart2, 0, part2Length);
                offset += part2Length;
            }

            // Auth plugin name (null-terminated string) - if CLIENT_PLUGIN_AUTH capability is set
            if ((packet.CapabilityFlags & (uint)ClientCapabilities.CLIENT_PLUGIN_AUTH) != 0)
            {
                var authPluginNameEnd = Array.IndexOf(payload, (byte)0, offset);
                if (authPluginNameEnd > offset)
                {
                    packet.AuthPluginName = Encoding.UTF8.GetString(payload, offset, authPluginNameEnd - offset);
                }
            }

            return packet;
        }
    }

    /// <summary>
    /// Client capability flags used in the handshake.
    /// Reference: https://dev.mysql.com/doc/internals/en/capability-flags.html
    /// </summary>
    [Flags]
    internal enum ClientCapabilities : uint
    {
        CLIENT_LONG_PASSWORD = 0x00000001,
        CLIENT_FOUND_ROWS = 0x00000002,
        CLIENT_LONG_FLAG = 0x00000004,
        CLIENT_CONNECT_WITH_DB = 0x00000008,
        CLIENT_NO_SCHEMA = 0x00000010,
        CLIENT_COMPRESS = 0x00000020,
        CLIENT_ODBC = 0x00000040,
        CLIENT_LOCAL_FILES = 0x00000080,
        CLIENT_IGNORE_SPACE = 0x00000100,
        CLIENT_PROTOCOL_41 = 0x00000200,
        CLIENT_INTERACTIVE = 0x00000400,
        CLIENT_SSL = 0x00000800,
        CLIENT_IGNORE_SIGPIPE = 0x00001000,
        CLIENT_TRANSACTIONS = 0x00002000,
        CLIENT_RESERVED = 0x00004000,
        CLIENT_SECURE_CONNECTION = 0x00008000,
        CLIENT_MULTI_STATEMENTS = 0x00010000,
        CLIENT_MULTI_RESULTS = 0x00020000,
        CLIENT_PS_MULTI_RESULTS = 0x00040000,
        CLIENT_PLUGIN_AUTH = 0x00080000,
        CLIENT_CONNECT_ATTRS = 0x00100000,
        CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA = 0x00200000,
        CLIENT_CAN_HANDLE_EXPIRED_PASSWORDS = 0x00400000,
        CLIENT_SESSION_TRACK = 0x00800000,
        CLIENT_DEPRECATE_EOF = 0x01000000
    }
}