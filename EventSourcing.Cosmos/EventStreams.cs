using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
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

		IStream stream = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredKeyedService<TStream>(serviceKey);
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
		IStream stream = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredKeyedService<TStream>(serviceKey);
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
			foreach (var @event in await iterator.ReadNextAsync())
			{
				state.Apply(@event.Data);
			}
		}

		return state;
	}
}