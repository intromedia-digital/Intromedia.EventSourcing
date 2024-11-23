using Microsoft.Azure.Cosmos;
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

        await cosmos.CreateDatabaseIfNotExistsAsync(options.DatabaseId);
        var db = cosmos.GetDatabase(options.DatabaseId);

        foreach (var stream in streams)
        {
            await db.CreateContainerIfNotExistsAsync(stream.Name, "/streamId");
        }
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

