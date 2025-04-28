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

        private readonly MySqlConnection _connection;

        public IReplicationClient Client { get; private set; }

        public static MySQLFixture Instance { get; } = new MySQLFixture();

        private MySQLFixture()
        {
            _connection = new MySqlConnection($"Server={_host};Database=garden;Uid={_username};Pwd={_password};");
            Client = new ReplicationClient();
            ConnectAsync().Wait();
        }

        private async Task ConnectAsync()
        {
            await _connection.OpenAsync();
            await Client.ConnectAsync(_host, _username, _password, 1);
        }

        public MySqlCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }

        public async Task<TLogEvent> ReceiveAsync<TLogEvent>(CancellationToken cancellationToken = default)
            where TLogEvent : LogEvent
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var logEvent = await Client.ReceiveAsync();

                if (logEvent is TLogEvent requiredLogEvent)
                {
                    return requiredLogEvent;
                }
            }

            return default;
        }

        public void Dispose()
        {
            Client?.CloseAsync().AsTask().Wait();
            _connection?.CloseAsync().Wait();
            _connection?.Dispose();
        }
    }
}