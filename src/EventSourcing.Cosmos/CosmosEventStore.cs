using Microsoft.Azure.Cosmos;
using OneOf.Types;
using OneOf;
using System.Net;
namespace EventSourcing.Cosmos;


internal sealed class CosmosEventStore : IEventStore
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseId;
    public CosmosEventStore(CosmosClient cosmosClient, string databaseId)
    {
        _cosmosClient = cosmosClient;
        _databaseId = databaseId;
    }
    public async Task<OneOf<Success, VersionMismatch, Unknown>> AppendToStreamAsync(string type, Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
    {
        var container = _cosmosClient.GetContainer(_databaseId, type);
        try
        {
            var response = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>(
                "appendEvent",
                new PartitionKey(streamId.ToString()),
                [events.Select(e => CosmosEventData.FromEvent(e, streamId, type))],
                cancellationToken: cancellationToken
            );
            return new Success();
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.BadRequest)
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
        var container = _cosmosClient.GetContainer(_databaseId, type);

        var query = new QueryDefinition("SELECT * FROM c WHERE c.streamId = @streamId AND c.version >= @fromVersion")
            .WithParameter("@streamId", streamId.ToString())
            .WithParameter("@fromVersion", fromVersion);

        var iterator = container.GetItemQueryIterator<CosmosEventData>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(streamId.ToString())
        });
        List<CosmosEventData> events = new();
        while (iterator.HasMoreResults)
        {
            FeedResponse<CosmosEventData> response = await iterator.ReadNextAsync(cancellationToken);
            events.AddRange(response);
        }
        return events.OrderBy(e => e.Version).Select(e => e.Data);
    }
    public async Task RemoveStreamAsync(string type, Guid streamId, CancellationToken cancellationToken = default)
    {
        var container = _cosmosClient.GetContainer(_databaseId, type);

        ResponseMessage response = await container.DeleteAllItemsByPartitionKeyStreamAsync(
            new PartitionKey(streamId.ToString()),
            cancellationToken: cancellationToken
            );

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to remove stream {streamId} from {type}.");
        }
    }
}
