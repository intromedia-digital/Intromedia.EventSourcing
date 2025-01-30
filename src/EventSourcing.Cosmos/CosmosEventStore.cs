using Microsoft.Azure.Cosmos;

internal sealed class CosmosEventStore : IEventStore
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseId;
    public CosmosEventStore(CosmosClient cosmosClient, string databaseId)
    {
        _cosmosClient = cosmosClient;
        _databaseId = databaseId;
    }
    public Task<IEventStream> OpenStream(string type, Guid streamId, CancellationToken cancellationToken = default)
    {
        var container = _cosmosClient.GetDatabase(_databaseId).GetContainer(type);
        return Task.FromResult<IEventStream>(new CosmosEventStream(container, streamId));
    }
}
