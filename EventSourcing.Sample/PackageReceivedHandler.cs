using Microsoft.Azure.Cosmos;

internal sealed class PackageEventHandler(CosmosClient cosmos) :
    IEventHandler<PackageReceived>,
    IEventHandler<PackageLoadedOnCart>,
    IEventHandler<PackageOutForDelivery>
{
    private readonly Container _projections = cosmos.GetContainer("event-sourcing", "packages");
    public async Task Handle(PackageReceived notification, CancellationToken cancellationToken)
    {
        var package = new PackageState
        {
            Id = notification.PackageId
        };
        package.Apply(notification);
        await _projections.UpsertItemAsync(package, new PartitionKey(package.Id.ToString()));
    }
    public async Task Handle(PackageLoadedOnCart notification, CancellationToken cancellationToken)
    {
        PackageState package = await _projections.ReadItemAsync<PackageState>(notification.PackageId.ToString(), new PartitionKey(notification.PackageId.ToString()));
        if (package is null)
            return;

        package.Apply(notification);
        await _projections.UpsertItemAsync(package, new PartitionKey(package.Id.ToString()));
    }
    public async Task Handle(PackageOutForDelivery notification, CancellationToken cancellationToken)
    {
        PackageState package = await _projections.ReadItemAsync<PackageState>(notification.PackageId.ToString(), new PartitionKey(notification.PackageId.ToString()));
        if (package is null)
            return;
        package.Apply(notification);
        await _projections.UpsertItemAsync(package, new PartitionKey(package.Id.ToString()));
    }
}
