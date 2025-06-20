using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SciSharp.MySQL.Replication.Protocol
{
    /// <summary>
    /// Direct MySQL connection implementation that handles the protocol without MySql.Data dependency.
    /// </summary>
    internal class DirectMySQLConnection : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private byte _sequenceId;
        private bool _isConnected;
        private string _serverVersion;
        private uint _connectionId;

        public bool IsConnected => _isConnected;
        public string ServerVersion => _serverVersion;
        public uint ConnectionId => _connectionId;
        public Stream Stream => _stream;

        /// <summary>
        /// Connects to the MySQL server and performs handshake authentication.
        /// </summary>
        public async Task ConnectAsync(string host, int port, string username, string password, string database = null)
        {
            if (_isConnected)
                throw new InvalidOperationException("Connection is already established");

            try
            {
                // Parse host and port
                var parts = host.Split(':');
                var hostName = parts[0];
                var portNumber = parts.Length > 1 ? int.Parse(parts[1]) : port;

                // Establish TCP connection
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(hostName, portNumber).ConfigureAwait(false);
                _stream = _tcpClient.GetStream();

                // Perform handshake
                await PerformHandshakeAsync(username, password, database).ConfigureAwait(false);

                _isConnected = true;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Performs the MySQL handshake protocol.
        /// </summary>
        private async Task PerformHandshakeAsync(string username, string password, string database)
        {
            // Read initial handshake packet from server
            var (handshakePayload, sequenceId) = await MySQLPacket.ReadFromStreamAsync(_stream).ConfigureAwait(false);
            _sequenceId = (byte)(sequenceId + 1);

            // Check for error packet
            if (handshakePayload[0] == 0xFF)
            {
                throw new Exception($"Server error during handshake: {ParseErrorPacket(handshakePayload)}");
            }

            // Parse handshake packet
            var handshake = HandshakePacket.Parse(handshakePayload);
            _serverVersion = handshake.ServerVersion;
            _connectionId = handshake.ConnectionId;

            // Generate auth response
            var authResponse = MySQLAuth.MySqlNativePassword(password, handshake.AuthPluginData);

            // Create handshake response
            var handshakeResponse = new HandshakeResponsePacket(username, authResponse, database, MySQLAuth.MySqlNativePasswordPlugin)
            {
                SequenceId = _sequenceId
            };

            // Send handshake response
            await handshakeResponse.WriteToStreamAsync(_stream).ConfigureAwait(false);
            _sequenceId++;

            // Read server response
            var (responsePayload, responseSequenceId) = await MySQLPacket.ReadFromStreamAsync(_stream).ConfigureAwait(false);
            _sequenceId = (byte)(responseSequenceId + 1);

            // Check response
            if (responsePayload[0] == 0xFF)
            {
                throw new Exception($"Authentication failed: {ParseErrorPacket(responsePayload)}");
            }
            else if (responsePayload[0] == 0x00)
            {
                // Success - OK packet
                return;
            }
            else
            {
                throw new Exception("Unexpected response during authentication");
            }
        }

        /// <summary>
        /// Executes a SQL query and returns the result set.
        /// </summary>
        public async Task<MySQLQueryResult> ExecuteQueryAsync(string query)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Connection is not established");

            // Reset sequence ID for new command
            _sequenceId = 0;

            // Send command packet
            var commandPacket = new CommandPacket(MySQLCommand.COM_QUERY, query)
            {
                SequenceId = _sequenceId
            };

            await commandPacket.WriteToStreamAsync(_stream).ConfigureAwait(false);
            _sequenceId++;

            // Read response packets
            var (responsePayload, responseSequenceId) = await MySQLPacket.ReadFromStreamAsync(_stream).ConfigureAwait(false);
            _sequenceId = (byte)(responseSequenceId + 1);

            // Check for error
            if (responsePayload[0] == 0xFF)
            {
                throw new Exception($"Query failed: {ParseErrorPacket(responsePayload)}");
            }

            // Check for OK packet (for non-SELECT queries)
            if (responsePayload[0] == 0x00)
            {
                return new MySQLQueryResult { IsSuccess = true };
            }

            // Parse result set
            return await ParseResultSetAsync(responsePayload).ConfigureAwait(false);
        }

        /// <summary>
        /// Parses a result set from query response.
        /// </summary>
        private async Task<MySQLQueryResult> ParseResultSetAsync(byte[] firstPacket)
        {
            var result = new MySQLQueryResult { IsSuccess = true };

            // First packet contains column count
            var columnCount = firstPacket[0];
            result.ColumnCount = columnCount;

            // Read column definition packets
            var columns = new List<MySQLColumn>();
            for (int i = 0; i < columnCount; i++)
            {
                var (columnPayload, sequenceId) = await MySQLPacket.ReadFromStreamAsync(_stream).ConfigureAwait(false);
                _sequenceId = (byte)(sequenceId + 1);

                var column = ParseColumnDefinition(columnPayload);
                columns.Add(column);
            }
            result.Columns = columns;

            // Read EOF packet after column definitions
            var (eofPayload, eofSequenceId) = await MySQLPacket.ReadFromStreamAsync(_stream).ConfigureAwait(false);
            _sequenceId = (byte)(eofSequenceId + 1);

            // Read data rows
            var rows = new List<MySQLRow>();
            while (true)
            {
                var (rowPayload, rowSequenceId) = await MySQLPacket.ReadFromStreamAsync(_stream).ConfigureAwait(false);
                _sequenceId = (byte)(rowSequenceId + 1);

                // Check for EOF packet
                if (rowPayload[0] == 0xFE && rowPayload.Length < 9)
                {
                    break;
                }

                var row = ParseRow(rowPayload, columnCount);
                rows.Add(row);
            }
            result.Rows = rows;

            return result;
        }

        /// <summary>
        /// Parses a column definition packet.
        /// </summary>
        private MySQLColumn ParseColumnDefinition(byte[] payload)
        {
            var column = new MySQLColumn();
            int offset = 0;

            // Skip catalog (length-encoded string)
            offset += ReadLengthEncodedString(payload, offset, out _);

            // Skip schema (length-encoded string)
            offset += ReadLengthEncodedString(payload, offset, out _);

            // Skip table (length-encoded string)
            offset += ReadLengthEncodedString(payload, offset, out _);

            // Skip org_table (length-encoded string)
            offset += ReadLengthEncodedString(payload, offset, out _);

            // Column name (length-encoded string)
            string columnName;
            offset += ReadLengthEncodedString(payload, offset, out columnName);
            column.Name = columnName;

            // Skip org_name (length-encoded string)
            offset += ReadLengthEncodedString(payload, offset, out _);

            // Skip length of fixed-length fields
            offset++;

            // Character set (2 bytes)
            offset += 2;

            // Column length (4 bytes)
            offset += 4;

            // Column type (1 byte)
            column.Type = payload[offset];

            return column;
        }

        /// <summary>
        /// Parses a data row packet.
        /// </summary>
        private MySQLRow ParseRow(byte[] payload, int columnCount)
        {
            var row = new MySQLRow();
            var values = new string[columnCount];
            int offset = 0;

            for (int i = 0; i < columnCount; i++)
            {
                offset += ReadLengthEncodedString(payload, offset, out values[i]);
            }

            row.Values = values;
            return row;
        }

        /// <summary>
        /// Reads a length-encoded string from the payload.
        /// </summary>
        private int ReadLengthEncodedString(byte[] payload, int offset, out string value)
        {
            if (payload[offset] == 0xFB)
            {
                // NULL value
                value = null;
                return 1;
            }

            var length = payload[offset];
            if (length < 0xFB)
            {
                // Single byte length
                value = Encoding.UTF8.GetString(payload, offset + 1, length);
                return 1 + length;
            }

            // Multi-byte length (not implemented for simplicity)
            throw new NotImplementedException("Multi-byte length-encoded strings not implemented");
        }

        /// <summary>
        /// Parses an error packet.
        /// </summary>
        private string ParseErrorPacket(byte[] payload)
        {
            if (payload.Length < 3)
                return "Unknown error";

            var errorCode = BitConverter.ToUInt16(payload, 1);
            var message = Encoding.UTF8.GetString(payload, 3, payload.Length - 3);
            return $"Error {errorCode}: {message}";
        }

        public void Dispose()
        {
            _isConnected = false;
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
    }

    /// <summary>
    /// Represents the result of a MySQL query.
    /// </summary>
    internal class MySQLQueryResult
    {
        public bool IsSuccess { get; set; }
        public int ColumnCount { get; set; }
        public List<MySQLColumn> Columns { get; set; }
        public List<MySQLRow> Rows { get; set; }
    }

    /// <summary>
    /// Represents a MySQL column definition.
    /// </summary>
    internal class MySQLColumn
    {
        public string Name { get; set; }
        public byte Type { get; set; }
    }

    /// <summary>
    /// Represents a MySQL data row.
    /// </summary>
    internal class MySQLRow
    {
        public string[] Values { get; set; }
    }
}