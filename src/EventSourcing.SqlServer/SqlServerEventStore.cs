
using Dapper;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventSourcing.SqlServer;
internal sealed class SqlServerEventStore(DbConnectionFactory dbConnectionFactory, JsonSerializerOptions jsonSerializerOptions) : IEventStore
{
    public async Task AppendToStreamAsync(string type, Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
    {
        // Make sure all events have the same stream id
        if (!events.All(e => e.StreamId.Equals(streamId)))
        {
            throw new ArgumentException("All events must have the same stream id");
        }

        using var dbConnection = dbConnectionFactory.CreateConnection();
        List<SqlEventData> sqlEvents = events.Select(e => SqlEventData.FromEvent(e, type, jsonSerializerOptions)).ToList();
        
        await dbConnection.ExecuteAsync(
            "INSERT INTO Streams (StreamType, Type, StreamId, Id, Version, Created, Data) VALUES (@StreamType, @Type, @StreamId, @Id, @Version, @Created, @Data)",
            sqlEvents
        );


    }
    public async Task<IEnumerable<IEvent>> ReadStreamAsync(string type, Guid streamId, CancellationToken cancellationToken = default)
    {
        return await ReadStreamAsync(type, streamId, 0, cancellationToken);
    }
    public async Task<IEnumerable<IEvent>> ReadStreamAsync(string type, Guid streamId, int fromVersion, CancellationToken cancellationToken = default)
    {
        using var dbConnection = dbConnectionFactory.CreateConnection();
        var events = await dbConnection.QueryAsync<SqlEventData>(
            "SELECT * FROM Streams WHERE StreamType = @StreamType AND StreamId = @StreamId AND Version >= @FromVersion ORDER BY Version",
            new { StreamType = type, StreamId = streamId, FromVersion = fromVersion }
        );
        return events.Select(e => e.ToEvent(jsonSerializerOptions));
    }
    public Task RemoveStreamAsync(string type, Guid streamId, CancellationToken cancellationToken = default)
    {
        using var dbConnection = dbConnectionFactory.CreateConnection();
        return dbConnection.ExecuteAsync(
            "DELETE FROM Streams WHERE StreamType = @StreamType AND StreamId = @StreamId",
            new { StreamType = type, StreamId = streamId }
        );
    }
}
