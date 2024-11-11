using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

internal sealed class SubscriptionReader<TSubscription>(IServiceScopeFactory serviceScopeFactory, CosmosClient cosmosClient, IOptions<CosmosDatabaseOptions> options) : IHostedService
    where TSubscription : Subscription
{
    private ChangeFeedProcessor changeFeedProcessor;
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var subscription = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<TSubscription>();

        var db = cosmosClient.GetDatabase(options.Value.DatabaseId);

        string leaseContainerId = $"{options.Value.StreamContainerId}_leases_{subscription.GetType().Name}";
        var leaseContainer = await db.CreateContainerIfNotExistsAsync(leaseContainerId, "/id");

        changeFeedProcessor = cosmosClient.GetContainer(options.Value.DatabaseId, options.Value.StreamContainerId)
            .GetChangeFeedProcessorBuilder<Event>(subscription.GetType().Name, (changes, ct) => HandleChanges(changes, subscription, ct))
            .WithInstanceName($"{Environment.MachineName}_{Environment.ProcessId}")
            .WithLeaseContainer(leaseContainer.Container)
            .WithStartTime(subscription.StartTime.ToUniversalTime())
            .WithMaxItems(1)
            .Build();

        await changeFeedProcessor.StartAsync();
    }
    public static async Task HandleChanges(IReadOnlyCollection<Event> changes, TSubscription subscription, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            var @event = JsonConvert.DeserializeObject(change.Payload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
            if (@event is IEvent)
            {
                // Check if the event is subscribed
                if (subscription.SubscribedEvents.Any(x => x == @event.GetType()))
                    await subscription.HandleEvent((IEvent)@event, cancellationToken);
            }
        }
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (changeFeedProcessor is not null)
            await changeFeedProcessor.StopAsync();
    }
}
