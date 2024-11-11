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
    public override async Task HandleEvent(IEvent @event, CancellationToken cancellationToken)
    {
        await _projection.Apply(@event);
    }
}