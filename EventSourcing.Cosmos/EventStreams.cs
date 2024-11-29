using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace EventSourcing.Cosmos;


internal sealed class EventStreams<TStream>(
	object serviceKey,
	CosmosClient cosmosClient, 
	CosmosDatabaseOptions options, 
	IServiceScopeFactory serviceScopeFactory) 
	: IAppendStream<TStream>
	where TStream : IStream
{
	public async Task Append(Guid streamId, params IEvent[] events)
	{
		if (events.Length == 0)
		{
			return;
		}

		var scope = serviceScopeFactory.CreateScope();
        IStream stream = scope.ServiceProvider.GetRequiredKeyedService<TStream>(serviceKey);
		var container = cosmosClient.GetContainer(options.DatabaseId, stream.Name);
		var transaction = container.CreateTransactionalBatch(new PartitionKey(streamId.ToString()));

		foreach (var @event in events)
		{
			transaction.CreateItem(new Event(streamId, @event));
		}

		await transaction.ExecuteAsync();
	}
	public async Task<TState> BuildState<TState>(Guid streamId)
		where TState : IState<TStream>, new()
	{
        var scope = serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventStreams<TStream>>>();
        IStream stream = scope.ServiceProvider.GetRequiredKeyedService<TStream>(serviceKey);
		var container = cosmosClient.GetContainer(options.DatabaseId, stream.Name);
		var partitionKey = new PartitionKey(streamId.ToString());

		QueryDefinition query = new QueryDefinition($"SELECT * FROM c WHERE c.streamId = @streamId ORDER BY c.Version ASC")
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
			FeedResponse<Event> response = await iterator.ReadNextAsync();
			logger.LogInformation("Read {Count} events from stream {StreamId} consuming {RU}", response.Count, streamId, response.RequestCharge);
            foreach (var @event in response.Resource)
			{
				state.Apply(@event.Data);
			}
		}

		return state;
	}
}