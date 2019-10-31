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
        private const byte CMD_DUMP_BINLOG = 0x12;
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

            LogEventPackageDecoder.RegisterEmptyPayloadEventTypes(
                    LogEventType.STOP_EVENT,
                    LogEventType.INTVAR_EVENT,
                    LogEventType.SLAVE_EVENT,
                    LogEventType.RAND_EVENT,
                    LogEventType.USER_VAR_EVENT,
                    LogEventType.XID_EVENT,
                    LogEventType.DELETE_ROWS_EVENT_V0,
                    LogEventType.UPDATE_ROWS_EVENT_V0,
                    LogEventType.WRITE_ROWS_EVENT_V0,
                    LogEventType.HEARTBEAT_LOG_EVENT);

            LogEventPackageDecoder.RegisterLogEventType<RotateLogEvent>(LogEventType.ROTATE_EVENT);
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
                var binlogInfo = await GetBinlogFileNameAndPosition(mysqlConn);

                //var binlogChecksum = await GetBinlogChecksum(mysqlConn);
                //LogEvent.ChecksumType = binlogChecksum;       

                _stream = GetStreamFromMySQLConnection(mysqlConn);

                await StartDumpBinlog(_stream, serverId, binlogInfo.Item1, binlogInfo.Item2);

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

        //https://dev.mysql.com/doc/refman/5.6/en/replication-howto-masterstatus.html
        private async Task<Tuple<string, int>> GetBinlogFileNameAndPosition(MySqlConnection mysqlConn)
        {
            var cmd = mysqlConn.CreateCommand();
            cmd.CommandText = "SHOW MASTER STATUS;";
            
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                    throw new Exception("No binlog information has been returned.");

                var fileName = reader.GetString(0);
                var position = reader.GetInt32(1);

                await reader.CloseAsync();

                return new Tuple<string, int>(fileName, position);
            }
        }

        private async Task<ChecksumType> GetBinlogChecksum(MySqlConnection mysqlConn)
        {
            var cmd = mysqlConn.CreateCommand();
            cmd.CommandText = "show global variables like 'binlog_checksum';";
            
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                    return ChecksumType.NONE;

                var checksumTypeName = reader.GetString(1).ToUpper();
                await reader.CloseAsync();

                return (ChecksumType)Enum.Parse(typeof(ChecksumType), checksumTypeName);
            }
        }

        /*
        https://dev.mysql.com/doc/internals/en/com-binlog-dump.html
        */
        private Memory<byte> GetDumpBinlogCommand(int serverId, string fileName, int position)
        {
            var fixPartSize = 15;
            var encoding = System.Text.Encoding.ASCII;
            var buffer = new byte[fixPartSize + encoding.GetByteCount(fileName) + 1];

            Span<byte> span = buffer;

            buffer[4] = CMD_DUMP_BINLOG;

            var n = span.Slice(5);
            BinaryPrimitives.WriteInt32LittleEndian(n, position);

            var flags = (short) (BINLOG_DUMP_NON_BLOCK | BINLOG_SEND_ANNOTATE_ROWS_EVENT);
            n = n.Slice(4);
            BinaryPrimitives.WriteInt16LittleEndian(n, flags);

            n = n.Slice(2);
            BinaryPrimitives.WriteInt32LittleEndian(n, serverId);

            var nameSpan = n.Slice(4);

            var len = encoding.GetBytes(fileName, nameSpan);

            len += fixPartSize;

            // end of the file name
            buffer[len++] = 0x00;

            var contentLen = len - 4;

            buffer[0] = (byte) (contentLen & 0xff);
            buffer[1] = (byte) (contentLen >> 8);
            buffer[2] = (byte) (contentLen >> 16);
            
            return new Memory<byte>(buffer, 0, len);
        }

        private async ValueTask StartDumpBinlog(Stream stream, int serverId, string fileName, int position)
        {
            var data = GetDumpBinlogCommand(serverId, fileName, position);
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

        public async ValueTask CloseAsync()
        {
            var connection = _connection;

            if (connection != null)
            {
                _connection = null;
                await connection.CloseAsync();
            }            
        }
    }
}