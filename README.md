# dotnet-mysql-replication

[![Build Status](https://api.travis-ci.org/SciSharp/dotnet-mysql-replication.svg?branch=master)](https://travis-ci.org/SciSharp/dotnet-mysql-replication)

C# Implementation of MySQL replication protocol. This allow you to receive event like insert, update, delete with their datas and raw SQL queries.

## Usage

```csharp

using SciSharp.MySQL.Replication;


var client = new ReplicationClient();
var result = await client.ConnectAsync("localhost", "root", "scisharp", 1);

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

//...

await client.CloseAsync();

```
