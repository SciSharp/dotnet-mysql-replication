using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SuperSocket.Client;

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
        /// Receives the next log event from the server.
        /// </summary>
        /// <returns>A task representing the asynchronous receive operation and containing the log event.</returns>
        ValueTask<LogEvent> ReceiveAsync();

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
