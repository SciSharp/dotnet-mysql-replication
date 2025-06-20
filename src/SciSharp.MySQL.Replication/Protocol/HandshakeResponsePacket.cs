using System;
using System.Collections.Generic;
using System.Text;

namespace SciSharp.MySQL.Replication.Protocol
{
    /// <summary>
    /// Represents the handshake response packet sent by the client.
    /// Reference: https://dev.mysql.com/doc/internals/en/connection-phase-packets.html#packet-Protocol::HandshakeResponse
    /// </summary>
    internal class HandshakeResponsePacket : MySQLPacket
    {
        public uint CapabilityFlags { get; set; }
        public uint MaxPacketSize { get; set; }
        public byte CharacterSet { get; set; }
        public string Username { get; set; }
        public byte[] AuthResponse { get; set; }
        public string Database { get; set; }
        public string AuthPluginName { get; set; }

        public HandshakeResponsePacket(string username, byte[] authResponse, string database = null, string authPluginName = "mysql_native_password")
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            AuthResponse = authResponse ?? throw new ArgumentNullException(nameof(authResponse));
            Database = database;
            AuthPluginName = authPluginName;
            
            // Set default capability flags for replication client
            CapabilityFlags = (uint)(
                ClientCapabilities.CLIENT_PROTOCOL_41 |
                ClientCapabilities.CLIENT_SECURE_CONNECTION |
                ClientCapabilities.CLIENT_LONG_PASSWORD |
                ClientCapabilities.CLIENT_TRANSACTIONS |
                ClientCapabilities.CLIENT_PLUGIN_AUTH |
                ClientCapabilities.CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA
            );
            
            if (!string.IsNullOrEmpty(database))
            {
                CapabilityFlags |= (uint)ClientCapabilities.CLIENT_CONNECT_WITH_DB;
            }
            
            MaxPacketSize = 0x01000000; // 16MB
            CharacterSet = 8; // latin1_swedish_ci
        }

        protected override byte[] GetPayload()
        {
            var payload = new List<byte>();

            // Capability flags (4 bytes)
            payload.AddRange(BitConverter.GetBytes(CapabilityFlags));

            // Max packet size (4 bytes)
            payload.AddRange(BitConverter.GetBytes(MaxPacketSize));

            // Character set (1 byte)
            payload.Add(CharacterSet);

            // Reserved (23 bytes of zeros)
            payload.AddRange(new byte[23]);

            // Username (null-terminated string)
            payload.AddRange(Encoding.UTF8.GetBytes(Username));
            payload.Add(0);

            // Auth response
            if ((CapabilityFlags & (uint)ClientCapabilities.CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA) != 0)
            {
                // Length-encoded auth response
                payload.Add((byte)AuthResponse.Length);
                payload.AddRange(AuthResponse);
            }
            else if ((CapabilityFlags & (uint)ClientCapabilities.CLIENT_SECURE_CONNECTION) != 0)
            {
                // Length-prefixed auth response
                payload.Add((byte)AuthResponse.Length);
                payload.AddRange(AuthResponse);
            }
            else
            {
                // Null-terminated auth response
                payload.AddRange(AuthResponse);
                payload.Add(0);
            }

            // Database (null-terminated string) - if CLIENT_CONNECT_WITH_DB is set
            if ((CapabilityFlags & (uint)ClientCapabilities.CLIENT_CONNECT_WITH_DB) != 0 && !string.IsNullOrEmpty(Database))
            {
                payload.AddRange(Encoding.UTF8.GetBytes(Database));
                payload.Add(0);
            }

            // Auth plugin name (null-terminated string) - if CLIENT_PLUGIN_AUTH is set
            if ((CapabilityFlags & (uint)ClientCapabilities.CLIENT_PLUGIN_AUTH) != 0 && !string.IsNullOrEmpty(AuthPluginName))
            {
                payload.AddRange(Encoding.UTF8.GetBytes(AuthPluginName));
                payload.Add(0);
            }

            return payload.ToArray();
        }
    }
}