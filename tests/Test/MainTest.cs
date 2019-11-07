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

                var cmd = mysqlConn.CreateCommand();
                cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death) values ('Rokie', 'Kerry', 'abc', 'F', '1982-04-20', '3000-01-01')";
                await cmd.ExecuteNonQueryAsync();

                await foreach (var eventLog in client.FetchEvents())
                {
                    Assert.NotNull(eventLog);
                    _outputHelper.WriteLine(eventLog.EventType.ToString());
                }
            }            

            await client.CloseAsync();
        }
    }
}
