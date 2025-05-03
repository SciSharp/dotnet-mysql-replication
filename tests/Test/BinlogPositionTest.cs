using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SciSharp.MySQL.Replication;
using Microsoft.Extensions.Logging;
using SciSharp.MySQL.Replication.Events;
using System.Threading;

namespace Test
{
    [Trait("Category", "Replication")]
    public class BinlogPositionTest
    {
        protected readonly ITestOutputHelper _outputHelper;

        public BinlogPositionTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        // Unit tests for BinlogPosition class
        [Fact]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Arrange
            string expectedFilename = "mysql-bin.000001";
            int expectedPosition = 4;

            // Act
            var binlogPosition = new BinlogPosition(expectedFilename, expectedPosition);

            // Assert
            Assert.Equal(expectedFilename, binlogPosition.Filename);
            Assert.Equal(expectedPosition, binlogPosition.Position);
        }

        [Fact]
        public void Constructor_WithNullFilename_ThrowsArgumentNullException()
        {
            // Arrange
            string filename = null;
            int position = 4;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new BinlogPosition(filename, position));
            Assert.Equal("filename", exception.ParamName);
        }

        [Fact]
        public void DefaultConstructor_ShouldCreateInstance()
        {
            // Act
            var binlogPosition = new BinlogPosition();
            
            // Assert
            Assert.Null(binlogPosition.Filename);
            Assert.Equal(0, binlogPosition.Position);
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            string filename = "mysql-bin.000001";
            int position = 4;
            var binlogPosition = new BinlogPosition(filename, position);
            string expected = $"{filename}:{position}";

            // Act
            string result = binlogPosition.ToString();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var binlogPosition = new BinlogPosition("original-bin.000001", 0);
            string newFilename = "new-bin.000002";
            int newPosition = 120;

            // Act
            binlogPosition.Filename = newFilename;
            binlogPosition.Position = newPosition;

            // Assert
            Assert.Equal(newFilename, binlogPosition.Filename);
            Assert.Equal(newPosition, binlogPosition.Position);
        }

        // Integration tests that connect to MySQL server
        [Fact]
        public async Task GetCurrentBinlogPosition_ReturnsValidPosition()
        {
            using var mysqlFixture = MySQLFixture.CreateMySQLFixture();

            // Execute a simple command to ensure we have binlog activity
            var cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync();
            
            // Get the current binlog position
            var client = mysqlFixture.Client;
            var currentPosition = client.CurrentPosition;
            
            // Assert
            Assert.NotNull(currentPosition);
            Assert.NotNull(currentPosition.Filename);
            Assert.True(currentPosition.Position > 0, "Binlog position should be greater than 0");
            _outputHelper.WriteLine($"Current binlog position: {currentPosition}");
        }
        
        [Fact]
        public async Task BinlogPosition_ShouldAdvanceAfterOperations()
        {
            using var mysqlFixture = MySQLFixture.CreateMySQLFixture();

            // Get the current binlog position to use as reference
            var client = mysqlFixture.Client;
            var initialPosition = client.CurrentPosition;
            _outputHelper.WriteLine($"Initial binlog position: {initialPosition}");
            
            // Execute an operation to advance binlog position
            var cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "INSERT INTO pet (name, owner, species) VALUES ('TestPet', 'TestOwner', 'TestSpecies')";
            await cmd.ExecuteNonQueryAsync();
            
            // Receive an event to ensure binlog has progressed
            var eventLog = await mysqlFixture.ReceiveAsync<WriteRowsEvent>();
            Assert.NotNull(eventLog);
            
            // Check that position has advanced
            var advancedPosition = client.CurrentPosition;
            _outputHelper.WriteLine($"Advanced binlog position: {advancedPosition}");
            
            // The position should have advanced (either by increasing the position number
            // or by moving to a new file if rotation occurred)
            Assert.True(
                advancedPosition.Position > initialPosition.Position || 
                advancedPosition.Filename != initialPosition.Filename,
                "Binlog position should have advanced after database operation"
            );
        }

        [Fact]
        public async Task PositionChanged_EventFires_WhenPositionChanges()
        {
            using var mysqlFixture = MySQLFixture.CreateMySQLFixture();

            // Set up event handler to detect position changes
            var client = mysqlFixture.Client;
            var positionChangedEvent = new ManualResetEventSlim(false);
            BinlogPosition capturedPosition = null;
            
            EventHandler<BinlogPosition> handler = (sender, position) => {
                capturedPosition = position;
                positionChangedEvent.Set();
            };
            
            client.PositionChanged += handler;
            
            try
            {
                // Execute an operation that will trigger binlog position change
                var cmd = mysqlFixture.CreateCommand();
                cmd.CommandText = "INSERT INTO pet (name, owner, species) VALUES ('EventTest', 'EventOwner', 'EventSpecies')";
                await cmd.ExecuteNonQueryAsync();

                await mysqlFixture.ReceiveAsync<WriteRowsEvent>();

                // Wait for the position changed event to fire
                var eventFired = positionChangedEvent.Wait(TimeSpan.FromSeconds(5));
                
                // Assert
                Assert.True(eventFired, "PositionChanged event should have fired");
                Assert.NotNull(capturedPosition);
                _outputHelper.WriteLine($"Position changed event fired with position: {capturedPosition}");
            }
            finally
            {
                // Clean up event handler
                client.PositionChanged -= handler;
            }
        }

        [Fact]
        public async Task RotateEvent_UpdatesBinlogPosition()
        {
            using var mysqlFixture = MySQLFixture.CreateMySQLFixture();

            // Force a log rotation
            var cmd = mysqlFixture.CreateCommand();
            cmd.CommandText = "FLUSH LOGS";
            await cmd.ExecuteNonQueryAsync();
            
            // We should receive a rotate event
            var rotateEvent = await mysqlFixture.ReceiveAsync<RotateEvent>();
            Assert.NotNull(rotateEvent);
            _outputHelper.WriteLine($"Received rotate event with next log: {rotateEvent.NextBinlogFileName} at position: {rotateEvent.RotatePosition}");
            
            // Get current position after rotation
            var currentPosition = mysqlFixture.Client.CurrentPosition;
            _outputHelper.WriteLine($"Current binlog position after rotation: {currentPosition}");
            
            // The rotate event's next binlog file name should match our current filename
            Assert.Equal(rotateEvent.NextBinlogFileName, currentPosition.Filename);
        }

        [Fact]
        public async Task ConnectWithBinlogPosition_ShouldStartFromSpecifiedPosition()
        {
            using var mysqlFixture = MySQLFixture.CreateMySQLFixture();

            // First get the current position from our existing connection to use as reference
            var currentPosition = mysqlFixture.Client.CurrentPosition;
            _outputHelper.WriteLine($"Current binlog position: {currentPosition}");
            
            // Create a new replication client
            var newClient = new ReplicationClient();
            
            try
            {
                // Connect with the specific binlog position
                var result = await newClient.ConnectAsync(
                    MySQLFixture.Host, // Same server as in MySQLFixture
                    MySQLFixture.Username,      // Same username as in MySQLFixture
                    MySQLFixture.Password,      // Same password as in MySQLFixture
                    2,           // Different server ID to avoid conflicts
                    currentPosition);

                // Verify connection was successful
                Assert.True(result.Result, $"Connection failed: {result.Message}");
                
                // Verify the position was set correctly
                Assert.NotNull(newClient.CurrentPosition);
                Assert.Equal(currentPosition.Filename, newClient.CurrentPosition.Filename);
                Assert.Equal(currentPosition.Position, newClient.CurrentPosition.Position);
                
                _outputHelper.WriteLine($"Successfully connected with position: {newClient.CurrentPosition}");
                
                // Now insert a record to generate a new event
                var cmd = mysqlFixture.CreateCommand();
                cmd.CommandText = "INSERT INTO pet (name, owner, species) VALUES ('PositionTest', 'PositionTestOwner', 'PositionTestSpecies')";
                await cmd.ExecuteNonQueryAsync();
                
                LogEvent eventLog = null;

                while (true)
                {
                    // Receive an event from the new client
                    eventLog = await newClient.ReceiveAsync();
                    Assert.NotNull(eventLog);
                    
                    if (eventLog is WriteRowsEvent)
                    {
                        break; // Exit loop if we receive a WriteRowsEvent
                    }
                }
                
                _outputHelper.WriteLine($"Received event of type {eventLog.EventType} at position {eventLog.Position}");
                
                // Verify the new client position has advanced
                var newPosition = newClient.CurrentPosition;
                _outputHelper.WriteLine($"New binlog position: {newPosition}");
                
                Assert.True(
                    newPosition.Position > currentPosition.Position || 
                    newPosition.Filename != currentPosition.Filename,
                    "Binlog position should have advanced after receiving an event"
                );
            }
            finally
            {
                // Clean up
                await newClient.CloseAsync();
            }
        }
    }
}