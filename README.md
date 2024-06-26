# dotnet-mysql-replication

[![build](https://github.com/SciSharp/dotnet-mysql-replication/actions/workflows/build.yaml/badge.svg)](https://github.com/SciSharp/dotnet-mysql-replication/actions/workflows/build.yaml)
[![MyGet Version](https://img.shields.io/myget/scisharp/vpre/SciSharp.MySQL.Replication)](https://www.myget.org/feed/scisharp/package/nuget/SciSharp.MySQL.Replication)
[![NuGet Version](https://img.shields.io/nuget/v/SciSharp.MySQL.Replication.svg?style=flat)](https://www.nuget.org/packages/SciSharp.MySQL.Replication/)

dotnet-mysql-replication is a C# Implementation of MySQL replication protocol client. This allows you to receive events like insert, update, delete with their data and raw SQL queries from MySQL.

## Usage

```csharp

using SciSharp.MySQL.Replication;

var serverHost = "localhost";
var username = "root";
var password = "scisharp";
var serverId =  1; // replication server id

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

//...

await client.CloseAsync();

```
