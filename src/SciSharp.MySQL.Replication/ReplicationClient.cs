using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Buffers.Binary;
using SuperSocket.Channel;
using Microsoft.Extensions.Logging;

namespace SciSharp.MySQL.Replication
{
    public class ReplicationClient : IReplicationClient
    {
        private const byte CMD_DUMP_BINLOG = 18;
        private const int BIN_LOG_HEADER_SIZE = 4;
        private const int BINLOG_DUMP_NON_BLOCK = 1;
        private const int BINLOG_SEND_ANNOTATE_ROWS_EVENT = 2;
        private MySqlConnection _connection;
        private Stream _stream;
        private PipeChannel<LogEvent> _pipeChannel;
        private ILogger _logger;

        public ReplicationClient()
        {
            using (var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole()))
            {
                _logger = loggerFactory.CreateLogger<ReplicationClient>();
            }
        }

        private Stream GetStreamFromMySQLConnection(MySqlConnection connection)
        {
            var driverField = connection.GetType().GetField("driver", BindingFlags.Instance | BindingFlags.NonPublic);
            var driver = driverField.GetValue(connection);
            var handlerField = driver.GetType().GetField("handler", BindingFlags.Instance | BindingFlags.NonPublic);
            var handler = handlerField.GetValue(driver);
            var baseStreamField = handler.GetType().GetField("baseStream", BindingFlags.Instance | BindingFlags.NonPublic);
            return baseStreamField.GetValue(handler) as Stream;
        }

        public async Task<LoginResult> ConnectAsync(string server, string username, string password, int serverId, string fileName)
        {
            var connString = $"Server={server}; UID={username}; Password={password}";

            var mysqlConn = new MySqlConnection(connString);

            try
            {
                await mysqlConn.OpenAsync();
            }
            catch (Exception e)
            {
                return new LoginResult
                {
                    Result = false,
                    Message = e.Message
                };
            }

            try
            {
                var cmd = mysqlConn.CreateCommand();
                cmd.CommandText = "SET @master_binlog_checksum='@@global.binlog_checksum'";
                await cmd.ExecuteNonQueryAsync();

                cmd = mysqlConn.CreateCommand();
                cmd.CommandText = "SET @mariadb_slave_capability='" + LogEvent.MARIA_SLAVE_CAPABILITY_MINE + "'";
                await cmd.ExecuteNonQueryAsync();

                _stream = GetStreamFromMySQLConnection(mysqlConn);

                await StartDumpBinlog(_stream, serverId, fileName);

                _connection = mysqlConn;
                return new LoginResult { Result = true };
            }
            catch (Exception e)
            {
                await mysqlConn.CloseAsync();
                
                return new LoginResult
                {
                    Result = false,
                    Message = e.Message
                };
            }
        }

        private Memory<byte> GetDumpBinlogCommand(int serverId, string fileName)
        {
            var fixPartSize = 11;
            var encoding = System.Text.Encoding.ASCII;
            var buffer = new byte[fixPartSize + encoding.GetByteCount(fileName)];

            Span<byte> span = buffer;

            span[0] = CMD_DUMP_BINLOG;

            var n = span.Slice(1);
            BinaryPrimitives.WriteInt32LittleEndian(n, BIN_LOG_HEADER_SIZE);

            var flags = (short) (BINLOG_DUMP_NON_BLOCK | BINLOG_SEND_ANNOTATE_ROWS_EVENT);
            n = n.Slice(4);
            BinaryPrimitives.WriteInt16LittleEndian(n, flags);

            n = n.Slice(2);
            BinaryPrimitives.WriteInt32LittleEndian(n, serverId);

            var nameSpan = n.Slice(fixPartSize);

            var len = encoding.GetBytes(fileName, nameSpan);

            len += fixPartSize;

             // What's this part?
            buffer[0] = (byte) (len & 0xff);
            buffer[1] = (byte) (len >> 8);
            buffer[2] = (byte) (len >> 16);

            return new Memory<byte>(buffer, 0, len);
        }

        private async ValueTask StartDumpBinlog(Stream stream, int serverId, string fileName)
        {
            var data = GetDumpBinlogCommand(serverId, fileName);
            await stream.WriteAsync(data);
            await stream.FlushAsync();
        }

        public IAsyncEnumerable<LogEvent> FetchEvents()
        {
            _pipeChannel = new StreamPipeChannel<LogEvent>(_stream, new LogEventPipelineFilter(), new ChannelOptions
                {
                    Logger = _logger
                });

            return _pipeChannel.RunAsync();
        }
    }
}