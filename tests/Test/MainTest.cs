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
    public class MainTest
    {
        protected readonly ITestOutputHelper _outputHelper;

        public MainTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task TestReceiveEvent()
        {
            using var mysqlFixture = MySQLFixture.CreateMySQLFixture();

            // insert
            var cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death) values ('Rokie', 'Kerry', 'abc', 'F', '1982-04-20', '3000-01-01'); SELECT LAST_INSERT_ID();";
            var id = (UInt64)(await cmd.ExecuteScalarAsync());

            // update
            cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "update pet set owner='Linda' where `id`=" + id;
            await cmd.ExecuteNonQueryAsync();

            // delete
            cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "delete from pet where `id`= " + id;
            await cmd.ExecuteNonQueryAsync();

            RowsEvent eventLog = await mysqlFixture.ReceiveAsync<WriteRowsEvent>();
            Assert.NotNull(eventLog);
            _outputHelper.WriteLine(eventLog.ToString() + "\r\n");

            eventLog = await mysqlFixture.ReceiveAsync<UpdateRowsEvent>();
            Assert.NotNull(eventLog);
            _outputHelper.WriteLine(eventLog.ToString() + "\r\n");

            eventLog = await mysqlFixture.ReceiveAsync<DeleteRowsEvent>();
            Assert.NotNull(eventLog);
            _outputHelper.WriteLine(eventLog.ToString() + "\r\n");
        }

        [Fact]
        public async Task TestInsertEvent()
        {
            using var mysqlFixture = MySQLFixture.CreateMySQLFixture();
            
            // insert
            var cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death, timeUpdated) values ('Rokie', 'Kerry', 'abc', 'F', '1992-05-20', '3000-01-01', now()); SELECT LAST_INSERT_ID();";
            var id = (UInt64)(await cmd.ExecuteScalarAsync());

            var eventLog = await mysqlFixture.ReceiveAsync<WriteRowsEvent>();

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
            using var mysqlFixture = MySQLFixture.CreateMySQLFixture();
            
            // insert
            var cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death, timeUpdated) values ('Rokie', 'Kerry', 'abc', 'F', '1992-05-20', '3000-01-01', now());";
            await cmd.ExecuteNonQueryAsync();

            // query
            cmd = mysqlFixture.CreateCommand();
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
            cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "update pet set owner='Linda', timeUpdated=now() where `id`=" + id;
            await cmd.ExecuteNonQueryAsync();

            var eventLog = await mysqlFixture.ReceiveAsync<UpdateRowsEvent>();

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

        [Fact]
        public async Task TestGetEventLogStream()
        {
            var mysqlFixture = MySQLFixture.CreateMySQLFixture();

            // Insert a new pet
            var cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death) values ('Buddy', 'Alex', 'dog', 'M', '2020-01-15', NULL); SELECT LAST_INSERT_ID();";
            var id = (UInt64)(await cmd.ExecuteScalarAsync());

            // Update the pet
            cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = $"UPDATE pet SET owner='Sarah' WHERE id={id}";
            await cmd.ExecuteNonQueryAsync();

            // Delete the pet
            cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = $"DELETE FROM pet WHERE id={id}";
            await cmd.ExecuteNonQueryAsync();

            // Use the client's GetEventLogStream to process events
            var eventCount = 0;
            var sawInsert = false;
            var sawUpdate = false;
            var sawDelete = false;

            // Process only the next 5 events (or fewer if we reach the end)
            await foreach (var logEvent in mysqlFixture.Client.GetEventLogStream())
            {
                eventCount++;
                _outputHelper.WriteLine($"Event type: {logEvent.EventType}");

                switch (logEvent)
                {
                    case WriteRowsEvent writeEvent:
                        _outputHelper.WriteLine($"INSERT event on table ID: {writeEvent.TableID}");
                        var insertRows = writeEvent.RowSet.ToReadableRows();
                        if (insertRows.Count > 0)
                        {
                            var petName = insertRows[0]["name"]?.ToString();
                            if (petName == "Buddy")
                            {
                                sawInsert = true;
                                _outputHelper.WriteLine($"Found INSERT for pet 'Buddy'");
                            }
                        }
                        break;
                        
                    case UpdateRowsEvent updateEvent:
                        _outputHelper.WriteLine($"UPDATE event on table ID: {updateEvent.TableID}");
                        var updateRows = updateEvent.RowSet.ToReadableRows();
                        if (updateRows.Count > 0)
                        {
                            var cellValue = updateRows[0]["owner"] as CellValue;
                            if (cellValue?.NewValue?.ToString() == "Sarah")
                            {
                                sawUpdate = true;
                                _outputHelper.WriteLine($"Found UPDATE with new owner 'Sarah'");
                            }
                        }
                        break;
                        
                    case DeleteRowsEvent deleteEvent:
                        _outputHelper.WriteLine($"DELETE event on table ID: {deleteEvent.TableID}");
                        var deleteRows = deleteEvent.RowSet.ToReadableRows();
                        if (deleteRows.Count > 0)
                        {
                            // For DELETE events, check if this might be our deleted pet
                            sawDelete = true;
                            _outputHelper.WriteLine($"Found DELETE event");
                        }
                        break;
                        
                    case QueryEvent queryEvent:
                        _outputHelper.WriteLine($"SQL Query: {queryEvent.Query}");
                        break;
                }

                // Exit the loop once we've seen all three events or processed 10 events
                if ((sawInsert && sawUpdate && sawDelete) || eventCount >= 20)
                {
                    break;
                }
            }

            // Assert that we saw all the events we expected
            Assert.True(sawInsert, "Should have seen INSERT event");
            Assert.True(sawUpdate, "Should have seen UPDATE event");
            Assert.True(sawDelete, "Should have seen DELETE event");
        }
    }
}
