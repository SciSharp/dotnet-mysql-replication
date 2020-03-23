using System;
using System.Linq;
using System.Collections.Generic;
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

        private async Task<LoginResult> ConnectAsync(ReplicationClient client)
        {
            return await client.ConnectAsync("localhost", "root", "scisharp", 1, "");
        }

        private MySqlConnection CreateConnection()
        {
            return new MySqlConnection("Server=localhost;Database=garden;Uid=root;Pwd=scisharp;");
        }
        
        [Fact]
        public async Task TestConnection()
        {
            var client = new ReplicationClient();
            var result = await ConnectAsync(client);
            Assert.True(result.Result, result.Message);
            await client.CloseAsync();
        }

        [Fact]
        public async Task TestReceiveEvent()
        {
            var client = new ReplicationClient();

            var result = await ConnectAsync(client);
            
            Assert.True(result.Result, result.Message);

            using (var mysqlConn = CreateConnection())
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

        [Fact]
        public async Task TestInsertEvent()
        {
            var client = new ReplicationClient();

            var result = await ConnectAsync(client);
            
            Assert.True(result.Result, result.Message);

            using (var mysqlConn = CreateConnection())
            {
                await mysqlConn.OpenAsync();

                // insert
                var cmd = mysqlConn.CreateCommand();
                cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death, timeUpdated) values ('Rokie', 'Kerry', 'abc', 'F', '1982-04-20', '3000-01-01', now()); SELECT LAST_INSERT_ID();";
                var id = (UInt64)(await cmd.ExecuteScalarAsync());

                await foreach (var eventLog in client.FetchEvents())
                {
                    if (eventLog.EventType == LogEventType.WRITE_ROWS_EVENT)
                    {
                        var log = eventLog as WriteRowsEvent;
                        Assert.NotNull(log);

                        var rows = log.RowSet.ToReadableRows();
                        Assert.Equal(1, rows.Count);

                        var row = rows[0];

                        Assert.Equal("Rokie", row["name"]);
                        Assert.Equal("Kerry", row["owner"]);
                        Assert.Equal("abc", row["species"]);
                        Assert.Equal("F", row["sex"]);

                        break;
                    }
                }
            }            

            await client.CloseAsync();
        }

        [Fact]
        public async Task TestUpdateEvent()
        {
            var client = new ReplicationClient();

            var result = await ConnectAsync(client);
            
            Assert.True(result.Result, result.Message);

            using (var mysqlConn = CreateConnection())
            {
                await mysqlConn.OpenAsync();

                // query
                var cmd = mysqlConn.CreateCommand();
                cmd.CommandText = "select * from pet order by `id` desc limit 1;";

                var oldValues = new Dictionary<string, object>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    Assert.True(await reader.ReadAsync());

                    for (var i = 0; i < reader.FieldCount ; i++)
                    {
                        oldValues.Add(reader.GetName(i), reader.GetValue(i));
                    }

                    await reader.CloseAsync();
                }

                var id = oldValues["id"];

                // update
                cmd = mysqlConn.CreateCommand();
                cmd.CommandText = "update pet set owner='Linda', timeUpdated=now() where `id`=" + id;
                await cmd.ExecuteNonQueryAsync();

                await foreach (var eventLog in client.FetchEvents())
                {
                    _outputHelper.WriteLine(eventLog.ToString() + "\r\n");
                    
                    if (eventLog.EventType == LogEventType.UPDATE_ROWS_EVENT)
                    {
                        var log = eventLog as UpdateRowsEvent;
                        Assert.NotNull(log);

                        var rows = log.RowSet.ToReadableRows();
                        Assert.Equal(1, rows.Count);

                        var row = rows[0];

                        var cellValue = row["id"] as CellValue;

                        Assert.Equal(id, cellValue.OldValue);
                        Assert.Equal(id, cellValue.NewValue);

                        cellValue = row["owner"] as CellValue;

                        Assert.Equal("Kerry", oldValues["owner"]);
                        Assert.Equal("Kerry", cellValue.OldValue);
                        Assert.Equal("Linda", cellValue.NewValue);

                        break;
                    }
                }
            }            

            await client.CloseAsync();
        }
    }
}
