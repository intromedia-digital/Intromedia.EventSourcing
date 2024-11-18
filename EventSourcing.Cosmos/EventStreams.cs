using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;


internal sealed class EventStreams(
    CosmosClient cosmosClient, 
    IOptions<CosmosDatabaseOptions> options, 
    IServiceScopeFactory serviceScopeFactory) 
    : IEventStreams
{
    public async Task Append<TStream>(Guid streamId, params IEvent[] events)
        where TStream : IStream
    {
        if (events.Length == 0)
        {
            return;
        }

        IStream stream = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<TStream>();
        var container = cosmosClient.GetContainer(options.Value.DatabaseId, stream.Name);
        var transaction = container.CreateTransactionalBatch(new PartitionKey(streamId.ToString()));

        foreach (var @event in events)
        {
            transaction.CreateItem(new Event(streamId, @event));
        }

        await transaction.ExecuteAsync();
    }
    public async Task<TState> BuildState<TStream, TState>(Guid streamId)
        where TStream : IStream
        where TState : IState<TStream>, new()
    {
        IStream stream = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<TStream>();
        var container = cosmosClient.GetContainer(options.Value.DatabaseId, stream.Name);
        var partitionKey = new PartitionKey(streamId.ToString());

        QueryDefinition query = new QueryDefinition($"SELECT * FROM c WHERE c.streamId = @streamId")
            .WithParameter("@streamId", streamId);

        var iterator = container.GetItemQueryIterator<Event>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = partitionKey
            }
        );

        var state = new TState()
        {
            Id = streamId
        };

        while (iterator.HasMoreResults)
        {
            foreach (var @event in await iterator.ReadNextAsync())
            {
                state.Apply(@event.Data);
            }
        }

        return state;
    }
}

