using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using MySql.Data.MySqlClient;
using SciSharp.MySQL.Replication;
using SciSharp.MySQL.Replication.Events;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Collections;
using System.Data;
using System.Globalization;

namespace Test
{
    [Trait("Category", "DataTypes")]
    public class DataTypesTest : IClassFixture<MySQLFixture>
    {
        private readonly MySQLFixture _mysqlFixture;

        public DataTypesTest(MySQLFixture mysqlFixture)
        {
            _mysqlFixture = mysqlFixture;
        }

        [Fact]
        public async Task TestDateTimeType()
        {
            var currentValue = DateTime.Now;
            currentValue = new DateTime(currentValue.Year, currentValue.Month, currentValue.Day, currentValue.Hour, currentValue.Minute, currentValue.Second);
            
            await TestDataType<DateTime>("datetime_table", currentValue, currentValue.AddDays(1), (reader, index) =>
            {
                return reader.GetDateTime(index);
            });
        }

        [Fact]
        public async Task TestIntType()
        {
            var currentValue = 42;
            await TestDataType<int>("int_table", currentValue, currentValue + 10, (reader, index) =>
            {
                return reader.GetInt32(index);
            });
        }

        [Fact]
        public async Task TestBigIntType()
        {
            var currentValue = 9223372036854775800L;
            await TestDataType<long>("bigint_table", currentValue, currentValue - 10, (reader, index) =>
            {
                return reader.GetInt64(index);
            });
        }

        [Fact]
        public async Task TestTinyIntType()
        {
            var currentValue = (sbyte)42;
            await TestDataType<sbyte>("tinyint_table", currentValue, (sbyte)(currentValue + 10), (reader, index) =>
            {
                return (sbyte)reader.GetByte(index);
            });
        }

        [Fact]
        public async Task TestSmallIntType()
        {
            var currentValue = (short)1000;
            await TestDataType<short>("smallint_table", currentValue, (short)(currentValue + 10), (reader, index) =>
            {
                return reader.GetInt16(index);
            });
        }

        [Fact]
        public async Task TestMediumIntType()
        {
            var currentValue = 8388000;  // Close to 2^23 limit for MEDIUMINT
            await TestDataType<int>("mediumint_table", currentValue, currentValue + 10, (reader, index) =>
            {
                return reader.GetInt32(index);
            });
        }

        [Fact]
        public async Task TestVarCharType()
        {
            var currentValue = "Hello World";
            await TestDataType<string>("varchar_table", currentValue, currentValue + " Updated", (reader, index) =>
            {
                return reader.GetString(index);
            });
        }

        //[Fact]
        public async Task TestDecimalType()
        {
            var currentValue = 123.45m;
            await TestDataType<decimal>("decimal_table", currentValue, currentValue + 10.55m, (reader, index) =>
            {
                return reader.GetDecimal(index);
            });
        }

        [Fact]
        public async Task TestFloatType()
        {
            var currentValue = 123.45f;
            await TestDataType<float>("float_table", currentValue, currentValue + 10.55f, (reader, index) =>
            {
                return reader.GetFloat(index);
            });
        }

        //[Fact]
        public async Task TestDoubleType()
        {
            var currentValue = 123456.789012;
            await TestDataType<double>("double_table", currentValue, currentValue + 100.123, (reader, index) =>
            {
                return reader.GetDouble(index);
            });
        }

        [Fact]
        public async Task TestDateType()
        {
            var currentValue = DateTime.Today;
            await TestDataType<DateTime>("date_table", currentValue, currentValue.AddDays(5), (reader, index) =>
            {
                return reader.GetDateTime(index).Date;
            });
        }

        //[Fact]
        public async Task TestTimeType()
        {
            var currentValue = new TimeSpan(10, 30, 45);
            await TestDataType<TimeSpan>("time_table", currentValue, currentValue.Add(new TimeSpan(1, 15, 30)), (reader, index) =>
            {
                return reader.GetTimeSpan(index);
            });
        }

        //[Fact]
        public async Task TestTimestampType()
        {
            var currentValue = DateTime.UtcNow;
            currentValue = new DateTime(currentValue.Year, currentValue.Month, currentValue.Day, currentValue.Hour, currentValue.Minute, currentValue.Second, DateTimeKind.Utc);
            
            await TestDataType<DateTime>("timestamp_table", currentValue, currentValue.AddHours(1), (reader, index) =>
            {
                return DateTime.SpecifyKind(reader.GetDateTime(index), DateTimeKind.Utc);
            });
        }

        //[Fact]
        public async Task TestBlobType()
        {
            var currentValue = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var updateValue = new byte[] { 0x06, 0x07, 0x08, 0x09, 0x0A };
            
            await TestDataType<byte[]>("blob_table", currentValue, updateValue, (reader, index) =>
            {
                var length = (int)reader.GetBytes(index, 0, null, 0, 0);
                var buffer = new byte[length];
                reader.GetBytes(index, 0, buffer, 0, length);
                return buffer;
            });
        }

        [Fact]
        public async Task TestEnumType()
        {
            var currentValue = "SMALL";
            await TestDataType<string>("enum_table", currentValue, "MEDIUM", (reader, index) =>
            {
                return reader.GetString(index);
            });
        }

        [Fact]
        public async Task TestSetType()
        {
            var currentValue = "RED,GREEN";
            await TestDataType<string>("set_table", currentValue, "RED,BLUE", (reader, index) =>
            {
                return reader.GetString(index);
            });
        }

        //[Fact]
        public async Task TestJsonType()
        {
            var currentValue = @"{""name"": ""John"", ""age"": 30}";
            var updateValue = @"{""name"": ""Jane"", ""age"": 25, ""city"": ""New York""}";
            
            await TestDataType<string>("json_table", currentValue, updateValue, (reader, index) =>
            {
                return reader.GetString(index);
            });
        }

        //[Fact]
        public async Task TestYearType()
        {
            var currentValue = 2023;
            await TestDataType<int>("year_table", currentValue, 2024, (reader, index) =>
            {
                return reader.GetInt16(index);
            });
        }

        private async Task TestDataType<TDateType>(string tableName, TDateType currentValue, TDateType updateValue, Func<MySqlDataReader, int, TDateType> dataReader)
        {
            // Insert a new row into the table
            var command = _mysqlFixture.CreateCommand();
            command.CommandText = $"insert into {tableName} (value) values (@value);SELECT LAST_INSERT_ID();";
            command.Parameters.AddWithValue("@value", currentValue);
            var id = (Int32)(UInt64)await command.ExecuteScalarAsync();

            // Validate the WriteRowsEvent
            var writeRowsEvent = await _mysqlFixture.ReceiveAsync<WriteRowsEvent>();

            Assert.Equal(1, writeRowsEvent.RowSet.Rows.Count);
            Assert.Equal("id", writeRowsEvent.RowSet.ColumnNames[0]);
            Assert.Equal("value", writeRowsEvent.RowSet.ColumnNames[1]);
            var idFromClient = writeRowsEvent.RowSet.Rows[0][0];
            var valueFromClient = writeRowsEvent.RowSet.Rows[0][1];
            Assert.Equal(id, (Int32)idFromClient);
            Assert.NotNull(valueFromClient);
            Assert.Equal(currentValue, (TDateType)valueFromClient);

            // Validate the data in the database with query
            command = _mysqlFixture.CreateCommand();
            command.CommandText = $"select value from {tableName} where id = @id";
            command.Parameters.AddWithValue("@id", id);

            MySqlDataReader reader = await command.ExecuteReaderAsync() as MySqlDataReader;

            Assert.True(await reader.ReadAsync());

            var savedValue = dataReader(reader, 0);
            await reader.CloseAsync();

            Assert.Equal(currentValue, savedValue);

            // Update the row
            command = _mysqlFixture.CreateCommand();
            command.CommandText = $"update {tableName} set value=@value where id = @id";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@value", updateValue);

            Assert.Equal(1, await command.ExecuteNonQueryAsync());

            // Validate the UpdateRowsEvent
            var updateRowsEvent = await _mysqlFixture.ReceiveAsync<UpdateRowsEvent>();
            Assert.Equal(1, updateRowsEvent.RowSet.Rows.Count);
            Assert.Equal("id", updateRowsEvent.RowSet.ColumnNames[0]); 
            Assert.Equal("value", updateRowsEvent.RowSet.ColumnNames[1]);
            var idCellValue = updateRowsEvent.RowSet.Rows[0][0] as CellValue;
            var valueCellValue = updateRowsEvent.RowSet.Rows[0][1] as CellValue;
            Assert.NotNull(idCellValue);
            Assert.Equal(id, (Int32)idCellValue.NewValue);
            Assert.Equal(id, (Int32)idCellValue.OldValue);
            Assert.NotNull(valueCellValue);
            Assert.Equal(currentValue, valueCellValue.OldValue);
            Assert.Equal(updateValue, valueCellValue.NewValue);

            // Delete the row
            command = _mysqlFixture.CreateCommand();
            command.CommandText = $"delete from {tableName} where id = @id";
            command.Parameters.AddWithValue("@id", id);

            await command.ExecuteNonQueryAsync();

            // Validate the DeleteRowsEvent
            var deleteRowsEvent = await _mysqlFixture.ReceiveAsync<DeleteRowsEvent>();
            Assert.Equal(1, deleteRowsEvent.RowSet.Rows.Count);
            Assert.Equal("id", deleteRowsEvent.RowSet.ColumnNames[0]);
            Assert.Equal("value", deleteRowsEvent.RowSet.ColumnNames[1]);
            idFromClient = deleteRowsEvent.RowSet.Rows[0][0];
            valueFromClient = deleteRowsEvent.RowSet.Rows[0][1];
            Assert.Equal(id, (Int32)idFromClient);
            Assert.NotNull(valueFromClient);
            Assert.Equal(updateValue, (TDateType)valueFromClient);
        }
    }
}