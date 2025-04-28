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
        private const string _host = "localhost";
        private const string _username = "root";
        private const string _password = "root";

        public IReplicationClient Client { get; private set; }

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public static MySQLFixture Instance { get; } = new MySQLFixture();

        private MySQLFixture()
        {
            Client = new ReplicationClient();
            ConnectAsync().Wait();
        }

        private async Task ConnectAsync()
        {
            await Client.ConnectAsync(_host, _username, _password, 1);
        }

        private MySqlConnection GetConnection()
        {
            var connection = new MySqlConnection($"Server={_host};Database=garden;Uid={_username};Pwd={_password};");
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