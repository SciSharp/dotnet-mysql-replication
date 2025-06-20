using System;
using System.Threading.Tasks;
using SciSharp.MySQL.Replication;
using Xunit;
using Xunit.Abstractions;

namespace Test
{
    [Trait("Category", "DirectConnection")]
    public class DirectConnectionTest
    {
        protected readonly ITestOutputHelper _outputHelper;

        public DirectConnectionTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }
        
        [Fact]
        public async Task TestReplicationClientWithDirectConnection()
        {
            var client = new ReplicationClient();
            
            try
            {
                // Test connection using the new implementation
                var result = await client.ConnectAsync("localhost", "root", "root", 1001);
                
                Assert.True(result.Result, $"Connection failed: {result.Message}");
                _outputHelper.WriteLine($"ReplicationClient connected successfully using direct MySQL protocol");
                _outputHelper.WriteLine($"Connection result message: {result.Message ?? "Success"}");
                
                // Verify current position is available
                Assert.NotNull(client.CurrentPosition);
                _outputHelper.WriteLine($"Current binlog position: {client.CurrentPosition.Filename}:{client.CurrentPosition.Position}");
                
                _outputHelper.WriteLine("Direct MySQL protocol implementation is working correctly!");
            }
            catch (Exception ex)
            {
                _outputHelper.WriteLine($"Test failed with exception: {ex.Message}");
                _outputHelper.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
            finally
            {
                await client.CloseAsync();
            }
        }
    }
}
