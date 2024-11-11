using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
internal sealed class Initializer(CosmosClient cosmos, IOptions<CosmosDatabaseOptions> options, IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var streams = scope.ServiceProvider.GetServices<IStream>();

        await cosmos.CreateDatabaseIfNotExistsAsync(options.Value.DatabaseId);
        var db = cosmos.GetDatabase(options.Value.DatabaseId);

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

