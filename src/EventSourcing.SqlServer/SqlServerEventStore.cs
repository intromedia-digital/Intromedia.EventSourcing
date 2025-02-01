
using Dapper;
using Microsoft.Data.SqlClient;
using OneOf;
using OneOf.Types;
using System.Data;
using System.Net;
using System.Text.Json;

namespace EventSourcing.SqlServer;
internal sealed class SqlServerEventStore(DbConnectionFactory dbConnectionFactory, JsonSerializerOptions jsonSerializerOptions) : IEventStore
{
    public async Task<OneOf<Success, VersionMismatch, Unknown>> AppendToStreamAsync(string type, Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
    {
        using var dbConnection = dbConnectionFactory.CreateConnection();
        List<SqlEventData> sqlEvents = events.Select(e => SqlEventData.FromEvent(e, streamId, type, jsonSerializerOptions)).ToList();
        try
        {
            await dbConnection.ExecuteAsync(
                "INSERT INTO Streams (StreamType, Type, StreamId, Id, Version, Created, Data) VALUES (@StreamType, @Type, @StreamId, @Id, @Version, @Created, @Data)",
                sqlEvents
            );
            return new Success();
        }
        catch (SqlException e) when (e.Number == 2601 || e.Number == 2627)
        {
            return VersionMismatch.Instance;
        }
        catch
        {
            return new Unknown();
        }
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
