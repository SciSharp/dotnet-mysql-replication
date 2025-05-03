using System;
using System.Threading.Tasks;
using SuperSocket.Client;
using SciSharp.MySQL.Replication.Events;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Interface for MySQL replication client that handles database event streaming.
    /// </summary>
    public interface IReplicationClient
    {
        /// <summary>
        /// Connects to a MySQL server with the specified credentials.
        /// </summary>
        /// <param name="server">The server address to connect to.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="serverId">The server ID to use for the replication client.</param>
        /// <returns>A task that represents the asynchronous login operation and contains the login result.</returns>
        Task<LoginResult> ConnectAsync(string server, string username, string password, int serverId);

        /// <summary>
        /// Connects to a MySQL server with the specified credentials and starts replication from a specific binlog position.
        /// </summary>
        /// <param name="server">The server address to connect to.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="serverId">The server ID to use for the replication client.</param>
        /// <param name="binlogPosition">The binary log position to start replicating from.</param>
        /// <returns>A task that represents the asynchronous login operation and contains the login result.</returns>
        Task<LoginResult> ConnectAsync(string server, string username, string password, int serverId, BinlogPosition binlogPosition);

        /// <summary>
        /// Gets the current binary log position.
        /// </summary>
        BinlogPosition CurrentPosition { get; }

        /// <summary>
        /// Event triggered when the binary log position changes.
        /// </summary>
        event EventHandler<BinlogPosition> PositionChanged;

        /// <summary>
        /// Receives the next log event from the server.
        /// </summary>
        /// <returns>A task representing the asynchronous receive operation and containing the log event.</returns>
        ValueTask<LogEvent> ReceiveAsync();

        /// <summary>
        /// Asynchronously streams log events from the server.
        /// This method will yield log events as they are received.
        /// </summary>
        IAsyncEnumerable<LogEvent> GetEventLogStream();

        /// <summary>
        /// Closes the connection to the server.
        /// </summary>
        /// <returns>A task representing the asynchronous close operation.</returns>
        ValueTask CloseAsync();

        /// <summary>
        /// Starts the continuous receiving of log events from the server.
        /// </summary>
        void StartReceive();

        /// <summary>
        /// Event triggered when a log event package is received.
        /// </summary>
        event PackageHandler<LogEvent> PackageHandler;
        
        /// <summary>
        /// Event triggered when the connection is closed.
        /// </summary>
        event EventHandler Closed;
    }
}
