using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
internal sealed class Initializer(object serviceKey, IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var streams = scope.ServiceProvider.GetKeyedServices<IStream>(serviceKey);
        CosmosClient cosmos = scope.ServiceProvider.GetRequiredKeyedService<CosmosClient>(serviceKey);
        CosmosDatabaseOptions options = scope.ServiceProvider.GetRequiredKeyedService<CosmosDatabaseOptions>(serviceKey);

        await cosmos.CreateDatabaseIfNotExistsAsync(options.DatabaseId, cancellationToken: cancellationToken);
        var db = cosmos.GetDatabase(options.DatabaseId);

        foreach (var stream in streams)
        {
            await db.CreateContainerIfNotExistsAsync(new ContainerProperties()
            {
                Id = stream.Name,
                PartitionKeyPath = "/streamId"
            }, cancellationToken: cancellationToken);
        }
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
