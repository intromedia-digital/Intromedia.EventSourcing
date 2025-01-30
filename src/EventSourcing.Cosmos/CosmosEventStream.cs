using Microsoft.Azure.Cosmos;

internal sealed class CosmosEventStream : IEventStream
{
    private readonly Container _container;
    private readonly Guid _streamId;
    public CosmosEventStream(Container container, Guid streamId)
    {
        _container = container;
        _streamId = streamId;
    }
    public async Task AppendToStreamAsync(IEnumerable<Event> events, CancellationToken cancellationToken = default)
    {
        IEnumerable<CosmosEventData> cosmosEvents = events.Select(e => new CosmosEventData(_streamId, e));
        var response = await _container.Scripts.ExecuteStoredProcedureAsync<dynamic>(
            "appendEvent",
            new PartitionKey(_streamId.ToString()),
            [cosmosEvents],
            cancellationToken: cancellationToken
        );
    }

    public async Task<IEnumerable<Event>> ReadStreamAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.streamId = @streamId")
            .WithParameter("@streamId", _streamId.ToString());
        var iterator = _container.GetItemQueryIterator<CosmosEventData>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(_streamId.ToString())
        });
        List<Event> events = new();
        while (iterator.HasMoreResults)
        {
            FeedResponse<CosmosEventData> response = await iterator.ReadNextAsync(cancellationToken);
            events.AddRange(response.Select(e => new Event(e.Id, e.Version, e.Data)));
        }
        return events.OrderBy(e => e.Version);
    }
}
