using System;
using System.Threading.Tasks;
using SciSharp.MySQL.Replication;
using Xunit;

namespace Test
{
    public class MainTest
    {
        public MainTest()
        {

        }
        
        [Fact]
        public async Task TestConnection()
        {
            var client = new ReplicationClient();

            var result = await client.ConnectAsync("localhost", "root", "scisharp", 1, "mysql-bin.log");
            
            Assert.True(result.Result, result.Message);

            await client.CloseAsync();
        }
    }
}
