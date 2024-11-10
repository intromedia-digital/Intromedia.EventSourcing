using Microsoft.Azure.Cosmos;

internal sealed class Initializer(CosmosClient cosmos) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await cosmos.CreateDatabaseIfNotExistsAsync("event-sourcing");
        await cosmos.GetDatabase("event-sourcing").CreateContainerIfNotExistsAsync("packages", "/id");
        await cosmos.GetDatabase("event-sourcing").CreateContainerIfNotExistsAsync("carts", "/id");
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

