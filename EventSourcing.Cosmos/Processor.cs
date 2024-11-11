using MediatR;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
internal sealed class Processor : IHostedService
{
    public Processor(CosmosClient cosmosClient, IOptions<CosmosDatabaseOptions> options, IServiceScopeFactory serviceScopeFactory)
    {
        ContainerResponse leases = cosmosClient.GetDatabase(options.Value.DatabaseId).CreateContainerIfNotExistsAsync(options.Value.StreamLeaseContainerId, "/id").Result;
        changeFeedProcessor = cosmosClient.GetContainer(options.Value.DatabaseId, options.Value.StreamContainerId)
        .GetChangeFeedProcessorBuilder<Event>("EventSourcingProcessor", (changes, ct) => HandleChanges(changes, serviceScopeFactory, ct))
        .WithInstanceName($"{Environment.MachineName}_{Environment.ProcessId}")
        .WithLeaseContainer(leases.Container)
        .Build();
    }
    private readonly ChangeFeedProcessor changeFeedProcessor;
    private async static Task HandleChanges(IReadOnlyCollection<Event> changes, IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IMediator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Processor>>();

        foreach (var change in changes)
        {
            var @event = JsonConvert.DeserializeObject(change.Payload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
            if (@event is IEvent)
            {
                try
                {
                    await publisher.Publish(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error handling event {@Event}", @event);
                }
            }
            else
                logger.LogWarning("Could not deserialize event in {@Change}", change);
        }
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await changeFeedProcessor.StartAsync();
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (changeFeedProcessor is not null)
            await changeFeedProcessor.StopAsync();
    }
}
