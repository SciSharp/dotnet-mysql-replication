using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    public interface IReplicationClient
    {
        Task<LoginResult> ConnectAsync(string server, string username, string password, int serverId, string fileName);

        IAsyncEnumerable<LogEvent> FetchEvents();

        ValueTask CloseAsync();
    }
}
