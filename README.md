# dotnet-mysql-replication

[![build](https://github.com/SciSharp/dotnet-mysql-replication/actions/workflows/build.yaml/badge.svg)](https://github.com/SciSharp/dotnet-mysql-replication/actions/workflows/build.yaml)
[![MyGet Version](https://img.shields.io/myget/scisharp/vpre/SciSharp.MySQL.Replication)](https://www.myget.org/feed/scisharp/package/nuget/SciSharp.MySQL.Replication)
[![NuGet Version](https://img.shields.io/nuget/v/SciSharp.MySQL.Replication.svg?style=flat)](https://www.nuget.org/packages/SciSharp.MySQL.Replication/)

A C# Implementation of MySQL replication protocol client

This library allows you to receive events like insert, update, delete with their data and raw SQL queries from MySQL.

## Features

- Connect to MySQL server as a replica
- Parse and process binary log events in real-time
- Support for all MySQL data types including JSON, BLOB, TEXT, etc.
- Handle various events including:
  - Query events (raw SQL statements)
  - Table maps
  - Row events (insert, update, delete)
  - Format description events
  - Rotate events
  - XID events (transaction identifiers)
- Checksum verification support
- Built-in support for MySQL binary format parsing
- Async/await first design
- Track and save binary log position 
- Start replication from a specific binary log position

## Requirements

- .NET 6.0+ or .NET Core 3.1+
- MySQL server with binary logging enabled
- MySQL user with replication privileges

## Installation

```
dotnet add package SciSharp.MySQL.Replication
```

## Basic Usage

```csharp
using SciSharp.MySQL.Replication;

var serverHost = "localhost";
var username = "root";
var password = "scisharp";
var serverId = 1; // replication server id

var client = new ReplicationClient();
var result = await client.ConnectAsync(serverHost, username, password, serverId);

if (!result.Result)
{
    Console.WriteLine($"Failed to connect: {result.Message}.");
    return;
}

client.PackageHandler += (s, p) =>
{
    Console.WriteLine(p.ToString());
    Console.WriteLine();
}

client.StartReceive();

// Keep your application running to receive events
// ...

await client.CloseAsync();
```

## Using Async Stream API

You can use the modern C# async stream pattern to process MySQL events using `GetEventLogStream()`:

```csharp
using SciSharp.MySQL.Replication;
using SciSharp.MySQL.Replication.Events;

var client = new ReplicationClient();
var result = await client.ConnectAsync("localhost", "root", "password", 1);

if (!result.Result)
{
    Console.WriteLine($"Failed to connect: {result.Message}.");
    return;
}

// Process events as they arrive using await foreach
await foreach (var logEvent in client.GetEventLogStream())
{
    switch (logEvent)
    {
        case WriteRowsEvent writeEvent:
            Console.WriteLine($"INSERT on table: {writeEvent.TableId}");
            break;
            
        case UpdateRowsEvent updateEvent:
            Console.WriteLine($"UPDATE on table: {updateEvent.TableId}");
            break;
            
        case QueryEvent queryEvent:
            Console.WriteLine($"SQL Query: {queryEvent.Query}");
            break;
            
        // Handle other event types as needed
    }
}

await client.CloseAsync();
```

This approach is useful for:
- Modern C# applications using .NET Core 3.0+
- Processing events sequentially in a more fluent, readable way
- Easier integration with async/await patterns
- Avoiding event handler callback complexity

## Position Tracking and Custom Starting Position

You can track the current binary log position and start from a specific position:

```csharp
using SciSharp.MySQL.Replication;

var client = new ReplicationClient();

// Track position changes
client.PositionChanged += (sender, position) =>
{
    Console.WriteLine($"Current position: {position}");
    // Save position to a file, database, etc.
    File.WriteAllText("binlog-position.txt", $"{position.Filename}:{position.Position}");
};

// Start from a specific position
var startPosition = new BinlogPosition("mysql-bin.000001", 4);
var result = await client.ConnectAsync("localhost", "root", "password", 1, startPosition);

// Get current position at any time
var currentPosition = client.CurrentPosition;
Console.WriteLine($"Current log file: {currentPosition.Filename}, position: {currentPosition.Position}");
```

## Advanced Usage

### Working with Specific Events

```csharp
using SciSharp.MySQL.Replication;
using SciSharp.MySQL.Replication.Events;

var client = new ReplicationClient();
// ... connect to MySQL

client.PackageHandler += (s, e) =>
{
    switch (e)
    {
        case WriteRowsEvent writeEvent:
            Console.WriteLine($"INSERT on table: {writeEvent.TableId}");
            foreach (var row in writeEvent.Rows)
            {
                // Process inserted rows
                foreach (var cell in row.Cells)
                {
                    Console.WriteLine($"  Column: {cell.ColumnIndex}, Value: {cell.Value}");
                }
            }
            break;
            
        case UpdateRowsEvent updateEvent:
            Console.WriteLine($"UPDATE on table: {updateEvent.TableId}");
            foreach (var row in updateEvent.Rows)
            {
                // Process before/after values for updated rows
                Console.WriteLine("  Before update:");
                foreach (var cell in row.BeforeUpdate)
                {
                    Console.WriteLine($"    Column: {cell.ColumnIndex}, Value: {cell.Value}");
                }
                Console.WriteLine("  After update:");
                foreach (var cell in row.AfterUpdate)
                {
                    Console.WriteLine($"    Column: {cell.ColumnIndex}, Value: {cell.Value}");
                }
            }
            break;
            
        case DeleteRowsEvent deleteEvent:
            Console.WriteLine($"DELETE on table: {deleteEvent.TableId}");
            foreach (var row in deleteEvent.Rows)
            {
                // Process deleted rows
                foreach (var cell in row.Cells)
                {
                    Console.WriteLine($"  Column: {cell.ColumnIndex}, Value: {cell.Value}");
                }
            }
            break;
            
        case QueryEvent queryEvent:
            Console.WriteLine($"SQL Query: {queryEvent.Query}");
            Console.WriteLine($"Database: {queryEvent.Schema}");
            break;

        case RotateEvent rotateEvent:
            Console.WriteLine($"Rotating to new binary log: {rotateEvent.NextBinlogFileName}");
            Console.WriteLine($"New position: {rotateEvent.RotatePosition}");
            break;
    }
};

client.StartReceive();
```

### Setting Up MySQL for Replication

1. Enable binary logging in your MySQL server's `my.cnf` or `my.ini`:

```
[mysqld]
server-id=1
log-bin=mysql-bin
binlog_format=ROW
```

2. Create a user with replication privileges:

```sql
CREATE USER 'replication_user'@'%' IDENTIFIED BY 'password';
GRANT REPLICATION SLAVE ON *.* TO 'replication_user'@'%';
FLUSH PRIVILEGES;
```

### Logging

You can provide a logger for detailed diagnostics:

```csharp
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
});

var client = new ReplicationClient();
client.Logger = loggerFactory.CreateLogger<ReplicationClient>();
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
