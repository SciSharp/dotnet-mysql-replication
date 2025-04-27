using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SciSharp.MySQL.Replication;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SciSharp.MySQL.Replication.Events;

namespace Test
{
    [Trait("Category", "Replication")]
    public class MainTest : IClassFixture<MySQLFixture>
    {
        private readonly MySQLFixture _mysqlFixture;

        protected readonly ITestOutputHelper _outputHelper;

        private readonly ILogger _logger;

        public MainTest(ITestOutputHelper outputHelper, MySQLFixture mysqlFixture)
        {
            _outputHelper = outputHelper;
            _mysqlFixture = mysqlFixture;
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<MainTest>();
        }

        [Fact]
        public async Task TestReceiveEvent()
        {
            // insert
            var cmd = _mysqlFixture.CreateCommand();
            cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death) values ('Rokie', 'Kerry', 'abc', 'F', '1982-04-20', '3000-01-01'); SELECT LAST_INSERT_ID();";
            var id = (UInt64)(await cmd.ExecuteScalarAsync());

            // update
            cmd = _mysqlFixture.CreateCommand();
            cmd.CommandText = "update pet set owner='Linda' where `id`=" + id;
            await cmd.ExecuteNonQueryAsync();

            // delete
            cmd = _mysqlFixture.CreateCommand();
            cmd.CommandText = "delete from pet where `id`= " + id;
            await cmd.ExecuteNonQueryAsync();

            while (true)
            {
                var eventLog = await _mysqlFixture.Client.ReceiveAsync();
                Assert.NotNull(eventLog);
                _outputHelper.WriteLine(eventLog.ToString() + "\r\n");

                if (eventLog is DeleteRowsEvent)
                    break;
            }
        }

        [Fact]
        public async Task TestInsertEvent()
        {
            // insert
            var cmd = _mysqlFixture.CreateCommand();
            cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death, timeUpdated) values ('Rokie', 'Kerry', 'abc', 'F', '1992-05-20', '3000-01-01', now()); SELECT LAST_INSERT_ID();";
            var id = (UInt64)(await cmd.ExecuteScalarAsync());

            var eventLog = await _mysqlFixture.ReceiveAsync<WriteRowsEvent>();

            Assert.NotNull(eventLog);

            var rows = eventLog.RowSet.ToReadableRows();
            Assert.Equal(1, rows.Count);

            var row = rows[0];

            Assert.Equal("Rokie", row["name"]);
            Assert.Equal("Kerry", row["owner"]);
            Assert.Equal("abc", row["species"]);
            Assert.Equal("F", row["sex"]);
        }

        [Fact]
        public async Task TestUpdateEvent()
        {
            // insert
            var cmd = _mysqlFixture.CreateCommand();
            cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death, timeUpdated) values ('Rokie', 'Kerry', 'abc', 'F', '1992-05-20', '3000-01-01', now());";
            await cmd.ExecuteNonQueryAsync();

            // query
            cmd = _mysqlFixture.CreateCommand();
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
            cmd = _mysqlFixture.CreateCommand();
            cmd.CommandText = "update pet set owner='Linda', timeUpdated=now() where `id`=" + id;
            await cmd.ExecuteNonQueryAsync();

            var eventLog = await _mysqlFixture.ReceiveAsync<UpdateRowsEvent>();

            _outputHelper.WriteLine(eventLog.ToString() + "\r\n");
            
            if (eventLog.EventType == LogEventType.UPDATE_ROWS_EVENT)
            {
                Assert.NotNull(eventLog);

                var rows = eventLog.RowSet.ToReadableRows();
                Assert.Equal(1, rows.Count);

                var row = rows[0];

                var cellValue = row["id"] as CellValue;

                Assert.Equal(id, cellValue.OldValue);
                Assert.Equal(id, cellValue.NewValue);

                cellValue = row["owner"] as CellValue;

                Assert.Equal("Kerry", oldValues["owner"]);
                Assert.Equal("Kerry", cellValue.OldValue);
                Assert.Equal("Linda", cellValue.NewValue);
            }
        }
    }
}
