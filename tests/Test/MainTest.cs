using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SciSharp.MySQL.Replication;
using Xunit;
using Xunit.Abstractions;

namespace Test
{
    [Trait("Category", "Replication")]
    public class MainTest
    {
        protected readonly ITestOutputHelper _outputHelper;

        public MainTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }
        
        [Fact]
        public async Task TestConnection()
        {
            var client = new ReplicationClient();

            var result = await client.ConnectAsync("localhost", "root", "scisharp", 1, "");
            
            Assert.True(result.Result, result.Message);

            await client.CloseAsync();
        }

        [Fact]
        public async Task TestReceiveEvent()
        {
            var client = new ReplicationClient();

            var result = await client.ConnectAsync("localhost", "root", "scisharp", 1, "");
            
            Assert.True(result.Result, result.Message);

            using (var mysqlConn = new MySqlConnection("Server=localhost;Database=garden;Uid=root;Pwd=scisharp;"))
            {
                await mysqlConn.OpenAsync();

                // insert
                var cmd = mysqlConn.CreateCommand();
                cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death) values ('Rokie', 'Kerry', 'abc', 'F', '1982-04-20', '3000-01-01'); SELECT LAST_INSERT_ID();";
                var id = (UInt64)(await cmd.ExecuteScalarAsync());

                // update
                cmd = mysqlConn.CreateCommand();
                cmd.CommandText = "update pet set owner='Linda' where `id`=" + id;
                await cmd.ExecuteNonQueryAsync();

                // delete
                cmd = mysqlConn.CreateCommand();
                cmd.CommandText = "delete from pet where `id`= " + id;
                await cmd.ExecuteNonQueryAsync();

                await foreach (var eventLog in client.FetchEvents())
                {
                    Assert.NotNull(eventLog);
                    _outputHelper.WriteLine(eventLog.ToString() + "\r\n");
                    
                    if (eventLog is DeleteRowsEvent)
                        break;
                }
            }            

            await client.CloseAsync();
        }
    }
}
