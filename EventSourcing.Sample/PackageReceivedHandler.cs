using Microsoft.Azure.Cosmos;

internal sealed class PackageReadModel
{
    public Guid Id { get; set; }
    public string TrackingNumber { get; set; }
    public Guid? CartId { get; set; }
    public DateTime? OutForDelivery { get; set; }
    public int Version { get; set; } = 0;
    public void Apply(IEvent @event)
    {
        switch (@event)
        {
            case PackageReceived e:
                Apply(e);
                break;
            case PackageLoadedOnCart e:
                Apply(e);
                break;
            case PackageOutForDelivery e:
                Apply(e);
                break;
        }
        Version = @event.Version;
    }
    private void Apply(PackageReceived @event)
    {
        TrackingNumber = @event.TrackingNumber;
    }
    private void Apply(PackageLoadedOnCart @event)
    {
        CartId = @event.CartId;
    }
    private void Apply(PackageOutForDelivery e)
    {

        OutForDelivery = e.Timestamp;
    }
}

internal sealed class PackageEventHandler(CosmosClient cosmos) :
IEventHandler<PackageReceived>,
IEventHandler<PackageLoadedOnCart>,
IEventHandler<PackageOutForDelivery>
{
    private readonly Container _projections = cosmos.GetContainer("event-sourcing", "packages");
    public async Task Handle(PackageReceived notification, CancellationToken cancellationToken)
    {
        var package = new PackageReadModel
        {
            Id = notification.PackageId
        };
        package.Apply(notification);
        await _projections.UpsertItemAsync(package, new PartitionKey(package.Id.ToString()));
    }
    public async Task Handle(PackageLoadedOnCart notification, CancellationToken cancellationToken)
    {
        PackageReadModel package = await _projections.ReadItemAsync<PackageReadModel>(notification.PackageId.ToString(), new PartitionKey(notification.PackageId.ToString()));
        if (package is null)
            return;

        package.Apply(notification);
        await _projections.UpsertItemAsync(package, new PartitionKey(package.Id.ToString()));
    }
    public async Task Handle(PackageOutForDelivery notification, CancellationToken cancellationToken)
    {
        PackageReadModel package = await _projections.ReadItemAsync<PackageReadModel>(notification.PackageId.ToString(), new PartitionKey(notification.PackageId.ToString()));
        if (package is null)
            return;
        package.Apply(notification);
        await _projections.UpsertItemAsync(package, new PartitionKey(package.Id.ToString()));
    }
}
