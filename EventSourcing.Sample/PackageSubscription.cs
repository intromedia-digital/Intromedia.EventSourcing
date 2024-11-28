internal class PackageSubscription: Subscription<PackageStream>
{
    private readonly PackageProjection _projection;
    public PackageSubscription(PackageProjection projection)
    {
        _projection = projection;

        StartFrom(DateTime.MinValue);
        Subscribe<PackageReceived>();
        Subscribe<PackageLoadedOnCart>();
        Subscribe<PackageOutForDelivery>();
    }
    public override async Task HandleEvent(Guid streamId, IEvent @event, CancellationToken cancellationToken)
    {
        await _projection.Apply(@event);
    }
}

internal class Package2Subscription : Subscription<PackageStream>
{
    private readonly PackageProjection _projection;
    public Package2Subscription(PackageProjection projection)
    {
        _projection = projection;

        StartFrom(DateTime.MinValue);
    }
    public override async Task HandleEvent(Guid streamId, IEvent @event, CancellationToken cancellationToken)
    {
        await _projection.Apply(@event);
    }
}