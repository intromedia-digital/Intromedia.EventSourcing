internal sealed class PackageState : IState
{
    #region IState implementation
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
    public Guid Id { get; set; }
    #endregion
    public string TrackingNumber { get; set; } = default!;
    public Guid? CartId { get; set; }
    public DateTime? DeliveryStarted { get; set; }
    public int Version { get; set; } = 0;
    public int NextVersion() => Version + 1;
    public void Apply(PackageReceived @event)
    {
        TrackingNumber = @event.TrackingNumber;
    }
    public void Apply(PackageLoadedOnCart @event)
    {
        if (CartId.HasValue)
        {
            throw new InvalidOperationException("Package is already loaded on a cart");
        }
        CartId = @event.CartId;
    }
    internal void Apply(PackageOutForDelivery e)
    {
        if(DeliveryStarted.HasValue)
        {
            throw new InvalidOperationException("Package is already out for delivery");
        }

        DeliveryStarted = e.Timestamp;
    }
}
