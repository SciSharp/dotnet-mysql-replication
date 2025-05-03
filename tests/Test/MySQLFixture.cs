using System;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SciSharp.MySQL.Replication;
using SciSharp.MySQL.Replication.Events;

namespace Test
{
    public class MySQLFixture : IDisposable
    {
        internal const string Host = "localhost";
        internal const string Username = "root";
        internal const string Password = "root";

        private readonly int _serverId;

        public IReplicationClient Client { get; private set; }

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        internal static MySQLFixture CreateMySQLFixture(int serverId = 1)
        {
            return new MySQLFixture(serverId);
        }

        private MySQLFixture(int serverId)
        {
            this._serverId = serverId;
            Client = new ReplicationClient();
            ConnectAsync().Wait();
        }

        private async Task ConnectAsync()
        {
            await Client.ConnectAsync(Host, Username, Password, _serverId);
        }

        private MySqlConnection GetConnection()
        {
            var connection = new MySqlConnection($"Server={Host};Database=garden;Uid={Username};Pwd={Password};");
            connection.OpenAsync().Wait();
            return connection;
        }

        public MySqlCommand CreateCommand()
        {
            return GetConnection().CreateCommand();
        }

        public async Task<TLogEvent> ReceiveAsync<TLogEvent>(CancellationToken cancellationToken = default)
            where TLogEvent : LogEvent
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var logEvent = await Client.ReceiveAsync();

                    if (logEvent is TLogEvent requiredLogEvent)
                    {
                        return requiredLogEvent;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return default;
        }

        public void Dispose()
        {
            Client?.CloseAsync().AsTask().Wait();
        }
    }
}