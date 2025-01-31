using Microsoft.Extensions.Hosting;

namespace EventSourcing.SqlServer;
internal sealed class SqlConfigureDatabase(DbConnectionFactory dbConnectionFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var dbConnection = dbConnectionFactory.CreateConnection();
        dbConnection.Open();
        using var command = dbConnection.CreateCommand();
        command.CommandText = @"
            IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'EventSourcing')
            BEGIN
                CREATE DATABASE EventSourcing;
            END
            USE EventSourcing;
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Streams')
            BEGIN
                CREATE TABLE Streams (
                    ClusterKey BIGINT IDENTITY(1,1) PRIMARY KEY,
                    Type NVARCHAR(255) NOT NULL,
                    StreamType NVARCHAR(255) NOT NULL,
                    Created DATETIME2 NOT NULL,
                    Version INT NOT NULL,
                    Id UNIQUEIDENTIFIER NOT NULL,
                    StreamId UNIQUEIDENTIFIER NOT NULL,
                    Data NVARCHAR(MAX) NOT NULL
                );

                CREATE INDEX IX_Streams_Type_Created ON Streams (Type, Created);
                CREATE INDEX IX_Streams_Created ON Streams (Created);
                CREATE UNIQUE INDEX IX_Streams_StreamId_Version ON Streams (StreamId, Version);

            END
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Subscriptions')
            BEGIN
                CREATE TABLE Subscriptions (
                    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    StreamType NVARCHAR(255) NOT NULL,
                    Name NVARCHAR(255) NOT NULL,
                    Continuation BIGINT NOT NULL
                );

                CREATE UNIQUE INDEX IX_Subscriptions_StreamType_Name ON Subscriptions (StreamType, Name);
                
            END
        ";
        command.ExecuteNonQuery();
        dbConnection.Close();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
