using Microsoft.Azure.Cosmos;
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
    public async Task AppendToStreamAsync(string type, Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
    {
        var container = _cosmosClient.GetContainer(_databaseId, type);
        var response = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>(
            "appendEvent",
            new PartitionKey(streamId.ToString()),
            [events],
            cancellationToken: cancellationToken
        );
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

        var iterator = container.GetItemQueryIterator<IEvent>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(streamId.ToString())
        });
        List<IEvent> events = new();
        while (iterator.HasMoreResults)
        {
            FeedResponse<IEvent> response = await iterator.ReadNextAsync(cancellationToken);
            events.AddRange(response);
        }
        return events.OrderBy(e => e.Version);
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
