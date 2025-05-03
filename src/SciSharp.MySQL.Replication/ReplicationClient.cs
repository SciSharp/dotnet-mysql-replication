using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using SciSharp.MySQL.Replication.Events;
using SuperSocket.Client;
using SuperSocket.Connection;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// A client that implements MySQL replication protocol to act as a replica.
    /// This allows reading binary log events from a MySQL server in real-time.
    /// </summary>
    public class ReplicationClient : EasyClient<LogEvent>, IReplicationClient
    {
        private const byte CMD_DUMP_BINLOG = 0x12;

        private const int BIN_LOG_HEADER_SIZE = 4;

        private const int BINLOG_DUMP_NON_BLOCK = 1;

        private const int BINLOG_SEND_ANNOTATE_ROWS_EVENT = 2;

        private MySqlConnection _connection;

        private int _serverId;

        private Stream _stream;

        private BinlogPosition _currentPosition;
        
        /// <summary>
        /// Gets the current binary log position.
        /// </summary>
        public BinlogPosition CurrentPosition => new BinlogPosition(_currentPosition);

        /// <summary>
        /// Event triggered when the binary log position changes.
        /// </summary>
        public event EventHandler<BinlogPosition> PositionChanged;

        /// <summary>
        /// Gets or sets the logger for the replication client.
        /// </summary>
        public new ILogger Logger
        {
            get { return base.Logger; }
            set { base.Logger = value; }
        }

        private readonly Dictionary<string, TableSchema> _tableSchemaMap;

        static ReplicationClient()
        {
            LogEventPackageDecoder.RegisterEmptyPayloadEventTypes(
                    LogEventType.STOP_EVENT,
                    LogEventType.INTVAR_EVENT,
                    LogEventType.SLAVE_EVENT,
                    LogEventType.RAND_EVENT,
                    LogEventType.USER_VAR_EVENT,
                    LogEventType.DELETE_ROWS_EVENT_V0,
                    LogEventType.UPDATE_ROWS_EVENT_V0,
                    LogEventType.WRITE_ROWS_EVENT_V0,
                    LogEventType.HEARTBEAT_LOG_EVENT,
                    LogEventType.ANONYMOUS_GTID_LOG_EVENT);

            LogEventPackageDecoder.RegisterLogEventType<RotateEvent>(LogEventType.ROTATE_EVENT);
            LogEventPackageDecoder.RegisterLogEventType<FormatDescriptionEvent>(LogEventType.FORMAT_DESCRIPTION_EVENT);
            LogEventPackageDecoder.RegisterLogEventType<TableMapEvent>(LogEventType.TABLE_MAP_EVENT);
            LogEventPackageDecoder.RegisterLogEventType<QueryEvent>(LogEventType.QUERY_EVENT);
            LogEventPackageDecoder.RegisterLogEventType<WriteRowsEvent>(LogEventType.WRITE_ROWS_EVENT);
            LogEventPackageDecoder.RegisterLogEventType<DeleteRowsEvent>(LogEventType.DELETE_ROWS_EVENT);
            LogEventPackageDecoder.RegisterLogEventType<UpdateRowsEvent>(LogEventType.UPDATE_ROWS_EVENT);
            LogEventPackageDecoder.RegisterLogEventType<XIDEvent>(LogEventType.XID_EVENT);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicationClient"/> class.
        /// </summary>
        public ReplicationClient()
            : this(new LogEventPipelineFilter())
        {
            
        }

        private ReplicationClient(LogEventPipelineFilter logEventPipelineFilter)
            : base(logEventPipelineFilter)
        {
            _tableSchemaMap = (logEventPipelineFilter.Context as ReplicationState).TableSchemaMap;
        }

        /// <summary>
        /// Gets the underlying stream from a MySQL connection.
        /// </summary>
        /// <param name="connection">The MySQL connection.</param>
        /// <returns>The stream associated with the connection.</returns>
        private Stream GetStreamFromMySQLConnection(MySqlConnection connection)
        {
            var driverField = connection.GetType().GetField("driver", BindingFlags.Instance | BindingFlags.NonPublic);
            var driver = driverField.GetValue(connection);
            var handlerField = driver.GetType().GetField("handler", BindingFlags.Instance | BindingFlags.NonPublic);
            var handler = handlerField.GetValue(driver);
            var baseStreamField = handler.GetType().GetField("baseStream", BindingFlags.Instance | BindingFlags.NonPublic);
            return baseStreamField.GetValue(handler) as Stream;
        }

        /// <summary>
        /// Connects to a MySQL server as a replication client.
        /// </summary>
        /// <param name="server">The server address.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="serverId">The server ID to use for this replication client.</param>
        /// <returns>A task representing the asynchronous operation, with a result indicating whether the login was successful.</returns>
        public async Task<LoginResult> ConnectAsync(string server, string username, string password, int serverId)
        {
            return await ConnectInternalAsync(server, username, password, serverId, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Connects to a MySQL server with the specified credentials and starts replication from a specific binlog position.
        /// </summary>
        /// <param name="server">The server address to connect to.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="serverId">The server ID to use for the replication client.</param>
        /// <param name="binlogPosition">The binary log position to start replicating from.</param>
        /// <returns>A task that represents the asynchronous login operation and contains the login result.</returns>
        public async Task<LoginResult> ConnectAsync(string server, string username, string password, int serverId, BinlogPosition binlogPosition)
        {
            if (binlogPosition == null)
                throw new ArgumentNullException(nameof(binlogPosition));

            return await ConnectInternalAsync(server, username, password, serverId, binlogPosition).ConfigureAwait(false);
        }

        /// <summary>
        /// Internal implementation of the connection logic for MySQL replication.
        /// </summary>
        /// <param name="server">The server address.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="serverId">The server ID.</param>
        /// <param name="binlogPosition">Optional binlog position to start from. If null, will use the current position from the server.</param>
        /// <returns>A task representing the asynchronous operation, with a result indicating whether the login was successful.</returns>
        private async Task<LoginResult> ConnectInternalAsync(string server, string username, string password, int serverId, BinlogPosition binlogPosition)
        {
            var connString = $"Server={server}; UID={username}; Password={password}";
            var mysqlConn = new MySqlConnection(connString);

            try
            {
                await mysqlConn.OpenAsync().ConfigureAwait(false);
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
                // Load database schema using the established connection
                await LoadDatabaseSchemaAsync(mysqlConn).ConfigureAwait(false);

                // If no binlog position was provided, get the current position from the server
                if (binlogPosition == null)
                {
                    binlogPosition = await GetBinlogFileNameAndPosition(mysqlConn).ConfigureAwait(false);
                }
                
                // Set up checksum verification
                var binlogChecksum = await GetBinlogChecksum(mysqlConn).ConfigureAwait(false);
                await ConfirmChecksum(mysqlConn).ConfigureAwait(false);
                LogEvent.ChecksumType = binlogChecksum;

                // Get the underlying stream and start the binlog dump
                _stream = GetStreamFromMySQLConnection(mysqlConn);
                _serverId = serverId;
                _currentPosition = new BinlogPosition(binlogPosition);

                await StartDumpBinlog(_stream, serverId, binlogPosition.Filename, binlogPosition.Position).ConfigureAwait(false);

                _connection = mysqlConn;

                // Create a connection for the event stream
                var connection = new StreamPipeConnection(
                    stream: _stream,
                    remoteEndPoint: null,
                    options: new ConnectionOptions
                    {
                        Logger = Logger
                    });
                
                // We no longer need to register the PackageHandler event
                // as we're overriding OnPackageReceived instead

                SetupConnection(connection);
                return new LoginResult { Result = true };
            }
            catch (Exception e)
            {
                await mysqlConn.CloseAsync().ConfigureAwait(false);
                
                return new LoginResult
                {
                    Result = false,
                    Message = e.Message
                };
            }
        }

        /// <summary>
        /// Override of the OnPackageReceived method to track binlog position changes
        /// </summary>
        /// <param name="package">The log event package received</param>
        /// <returns>A ValueTask representing the asynchronous operation</returns>
        protected override ValueTask OnPackageReceived(LogEvent package)
        {
            // Track position for events coming through the event pipeline
            TrackBinlogPosition(package);
            
            // Call base implementation to allow normal event handling
            return base.OnPackageReceived(package);
        }
        
        /// <summary>
        /// Updates the position tracking based on the received log event.
        /// </summary>
        /// <param name="logEvent">The log event to process for position tracking.</param>
        /// <returns>True if the position was updated, false otherwise.</returns>
        private void TrackBinlogPosition(LogEvent logEvent)
        {
            if (logEvent == null || _currentPosition == null)
                return;

            if (logEvent is RotateEvent rotateEvent)
            {
                // For rotate events, we need to create a new position with the new filename                
                _currentPosition = new BinlogPosition(
                    rotateEvent.NextBinlogFileName,
                    (int)rotateEvent.RotatePosition);
                
                PositionChanged?.Invoke(this, _currentPosition);
            }
            else 
            {
                // For other events, just update the position number without creating a new object
                int newPos = logEvent.Position + logEvent.EventSize;
                
                if (_currentPosition.Position != newPos)
                {
                    // Only update if position has actually changed
                    _currentPosition.Position = newPos;
                    PositionChanged?.Invoke(this, _currentPosition);
                }
            }
        }

        private async Task LoadDatabaseSchemaAsync(MySqlConnection mysqlConn)
        {
            var tableSchemaTable = await mysqlConn.GetSchemaAsync("Columns").ConfigureAwait(false);

            var systemDatabases = new HashSet<string>(
                new [] { "mysql", "information_schema", "performance_schema", "sys" },
                StringComparer.OrdinalIgnoreCase);

            var userDatabaseColumns = tableSchemaTable.Rows.OfType<DataRow>()
                .Where(row => !systemDatabases.Contains(row.ItemArray[1].ToString()))
                .ToArray();

            userDatabaseColumns.Select(row =>
                {
                    var columnSizeCell = row["CHARACTER_MAXIMUM_LENGTH"];
                
                    return new {
                        TableName = row["TABLE_NAME"].ToString(),
                        DatabaseName = row["TABLE_SCHEMA"].ToString(),
                        ColumnName = row["COLUMN_NAME"].ToString(),
                        ColumnType = row["DATA_TYPE"].ToString(),
                        ColumnSize = columnSizeCell == DBNull.Value ? 0 : Convert.ToUInt64(columnSizeCell),
                    };
                })
                .GroupBy(row => new { row.TableName, row.DatabaseName })
                .ToList()
                .ForEach(group =>
                {
                    var tableSchema = new TableSchema
                    {
                        TableName = group.Key.TableName,
                        DatabaseName = group.Key.DatabaseName,
                        Columns = group.Select(row => new ColumnSchema
                        {
                            Name = row.ColumnName,
                            DataType = row.ColumnType,
                            ColumnSize = row.ColumnSize
                        }).ToList()
                    };

                    _tableSchemaMap[$"{group.Key.DatabaseName}.{group.Key.TableName}"] = tableSchema;
                });
        }

        /// <summary>
        /// Retrieves the binary log file name and position from the MySQL server.
        /// https://dev.mysql.com/doc/refman/5.6/en/replication-howto-masterstatus.html
        /// </summary>
        /// <param name="mysqlConn">The MySQL connection.</param>
        /// <returns>A tuple containing the binary log file name and position.</returns>
        private async Task<BinlogPosition> GetBinlogFileNameAndPosition(MySqlConnection mysqlConn)
        {
            var cmd = mysqlConn.CreateCommand();
            cmd.CommandText = "SHOW MASTER STATUS;";
            
            using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
            {
                if (!await reader.ReadAsync())
                    throw new Exception("No binlog information has been returned.");

                var fileName = reader.GetString(0);
                var position = reader.GetInt32(1);

                await reader.CloseAsync().ConfigureAwait(false);

                return new BinlogPosition(fileName, position);
            }
        }

        /// <summary>
        /// Retrieves the binary log checksum type from the MySQL server.
        /// </summary>
        /// <param name="mysqlConn">The MySQL connection.</param>
        /// <returns>The checksum type.</returns>
        private async Task<ChecksumType> GetBinlogChecksum(MySqlConnection mysqlConn)
        {
            var cmd = mysqlConn.CreateCommand();
            cmd.CommandText = "show global variables like 'binlog_checksum';";
            
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync().ConfigureAwait(false))
                    return ChecksumType.NONE;

                var checksumTypeName = reader.GetString(1).ToUpper();
                await reader.CloseAsync().ConfigureAwait(false);

                return (ChecksumType)Enum.Parse(typeof(ChecksumType), checksumTypeName);
            }
        }
        
        /// <summary>
        /// Confirms the binary log checksum setting on the MySQL server.
        /// </summary>
        /// <param name="mysqlConn">The MySQL connection.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async ValueTask ConfirmChecksum(MySqlConnection mysqlConn)
        {
            var cmd = mysqlConn.CreateCommand();
            cmd.CommandText = "set @`master_binlog_checksum` = @@binlog_checksum;";        
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Constructs the binary log dump command.
        /// https://dev.mysql.com/doc/internals/en/com-binlog-dump.html
        /// </summary>
        /// <param name="serverId">The server ID.</param>
        /// <param name="fileName">The binary log file name.</param>
        /// <param name="position">The position in the binary log file.</param>
        /// <returns>A memory buffer containing the command.</returns>
        private Memory<byte> GetDumpBinlogCommand(int serverId, string fileName, int position)
        {
            var fixPartSize = 15;
            var encoding = System.Text.Encoding.ASCII;
            var buffer = new byte[fixPartSize + encoding.GetByteCount(fileName) + 1];

            Span<byte> span = buffer;

            buffer[4] = CMD_DUMP_BINLOG;

            var n = span.Slice(5);
            BinaryPrimitives.WriteInt32LittleEndian(n, position);

            var flags = (short)0;
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

        /// <summary>
        /// Starts the binary log dump process.
        /// </summary>
        /// <param name="stream">The stream to write the command to.</param>
        /// <param name="serverId">The server ID.</param>
        /// <param name="fileName">The binary log file name.</param>
        /// <param name="position">The position in the binary log file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async ValueTask StartDumpBinlog(Stream stream, int serverId, string fileName, int position)
        {
            var data = GetDumpBinlogCommand(serverId, fileName, position);
            await stream.WriteAsync(data).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Starts receiving log event packages from the server.
        /// </summary>
        public void StartReceive()
        {
            _ = this.StartReceiveAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Logger?.LogError(task.Exception, "Error receiving log event package.");
                }
            });
        }

        /// <summary>
        /// Receives a log event asynchronously.
        /// </summary>
        /// <returns>The received log event.</returns>
        public new async ValueTask<LogEvent> ReceiveAsync()
        {
            var logEvent = await base.ReceiveAsync();
            
            // Update position tracking for events received directly through ReceiveAsync
            if (logEvent != null)
            {
                TrackBinlogPosition(logEvent);
            }
            
            return logEvent;
        }

        /// <summary>
        /// Asynchronously streams log events from the server.
        /// This method will yield log events as they are received.
        /// </summary>
        public async IAsyncEnumerable<LogEvent> GetEventLogStream()
        {
            while (true)
            {
                var logEvent = await ReceiveAsync();

                if (logEvent == null)
                    break;

                yield return logEvent;
            }
        }

        /// <summary>
        /// Closes the connection to the MySQL server.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async ValueTask CloseAsync()
        {
            var connection = _connection;

            if (connection != null)
            {
                _connection = null;
                await connection.CloseAsync().ConfigureAwait(false);
            }

            await base.CloseAsync().ConfigureAwait(false);
        }
    }
}