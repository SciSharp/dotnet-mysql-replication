using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Reflection;

namespace SciSharp.MySQL.Replication
{
    public class ReplicationClient : IReplicationClient
    {
        private MySqlConnection _connection;

        private Stream _stream;

        private Stream GetStreamFromMySQLConnection(MySqlConnection connection)
        {
            var driverField = connection.GetType().GetField("driver", BindingFlags.Instance | BindingFlags.NonPublic);
            var driver = driverField.GetValue(connection);
            var handlerField = driver.GetType().GetField("handler", BindingFlags.Instance | BindingFlags.NonPublic);
            var handler = handlerField.GetValue(driver);
            var baseStreamField = handler.GetType().GetField("baseStream", BindingFlags.Instance | BindingFlags.NonPublic);
            return baseStreamField.GetValue(handler) as Stream;
        }

        public async Task<LoginResult> ConnectAsync(string server, string username, string password)
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
                _stream = GetStreamFromMySQLConnection(mysqlConn);
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

        public IAsyncEnumerable<LogEvent> FetchEvents()
        {
            throw new NotImplementedException();
        }
    }
}