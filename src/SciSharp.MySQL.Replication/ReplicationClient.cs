using System;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace SciSharp.MySQL.Replication
{
    public class ReplicationClient : IReplicationClient
    {
        public Task<LoginResult> ConnectAsync(string server, string username, string password)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<LogEvent> FetchEvents()
        {
            throw new NotImplementedException();
        }
    }
}