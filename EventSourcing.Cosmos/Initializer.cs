using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

internal sealed class Initializer(CosmosClient cosmos, IOptions<CosmosDatabaseOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await cosmos.CreateDatabaseIfNotExistsAsync(options.Value.DatabaseId);
        await cosmos.GetDatabase(options.Value.DatabaseId).CreateContainerIfNotExistsAsync(options.Value.StreamContainerId, "/streamId");
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

