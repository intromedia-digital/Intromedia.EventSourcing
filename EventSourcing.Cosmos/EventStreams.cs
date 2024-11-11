using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
internal sealed class EventStreams(CosmosClient cosmosClient, IOptions<CosmosDatabaseOptions> options) : IEventStreams
{
    private readonly Container _container = cosmosClient.GetContainer(options.Value.DatabaseId, options.Value.StreamContainerId);
    public async Task Append(Guid streamId, string streamType, params IEvent[] events)
    {
        var partitionKeyBuilder = new PartitionKeyBuilder();
        partitionKeyBuilder.Add(streamType);
        partitionKeyBuilder.Add(streamId.ToString());
        var transaction = _container.CreateTransactionalBatch(partitionKeyBuilder.Build());

        foreach (var @event in events)
        {
            transaction.CreateItem(new Event(streamId, streamType, @event));
        }

        await transaction.ExecuteAsync();
    }
    public async Task<TState> BuildState<TState>(Guid streamId) where TState : IState, new()
    {
        var partitionKey = new PartitionKey(streamId.ToString());
        var iterator = _container.GetItemLinqQueryable<Event>(requestOptions: new QueryRequestOptions { PartitionKey = partitionKey })
            .Where(e => e.StreamId == streamId)
            .OrderBy(e => e.Timestamp)
            .ToFeedIterator();

        var state = new TState()
        {
            Id = streamId
        };

        while (iterator.HasMoreResults)
        {
            foreach (var @event in await iterator.ReadNextAsync())
            {
                var data = JsonConvert.DeserializeObject(@event.Payload, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                if (data is IEvent evnt)
                {
                    state.Apply(evnt);
                }
            }
        }

        return state;
    }

    public async Task<ReadStream> Get(string streamType, int offset)
    {
        FeedIterator<Event> iterator;


        iterator = _container.GetItemLinqQueryable<Event>(
           requestOptions: new QueryRequestOptions
           {
               PartitionKey = new PartitionKey(streamType),
               MaxItemCount = 1
           })
            .Where(e => e.StreamType == streamType)
            .OrderBy(e => e.Timestamp)
            .Skip(offset)
            .ToFeedIterator();

        var events = new List<IEvent>();

        string continuation = string.Empty;

        var response = await iterator.ReadNextAsync();
        continuation = response.ContinuationToken;
        foreach (var @event in response)
        {
            var data = JsonConvert.DeserializeObject(@event.Payload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            if (data is IEvent evnt)
            {
                events.Add(evnt);
            }
        }

        return new ReadStream(offset + events.Count(), events);
    }
}

