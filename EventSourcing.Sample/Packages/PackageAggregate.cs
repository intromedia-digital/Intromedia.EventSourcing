
internal sealed class PackageAggregate
{
    #region Constructor
    public PackageAggregate(Guid id, string trackingNumber)
    {
        _state = new PackageState
        {
            Id = id,
        };
        var e = new PackageReceived
        {
            PackageId = id,
            TrackingNumber = trackingNumber,
            Version = _state.Version
        };
        _state.Apply(e);
        _events.Add(e);
    }
    public PackageAggregate(PackageState state)
    {
        _state = state;
    }
    #endregion
    #region Data & Events
    private readonly PackageState _state;
    private readonly List<IEvent> _events = new();
    public Guid Id => _state.Id;
    public IReadOnlyCollection<IEvent> PopEvents()
    {
        var events = _events.ToList().AsReadOnly();
        _events.Clear();
        return events;
    }
    #endregion
    public void LoadOnCart(Guid cartId)
    {
        var e = new PackageLoadedOnCart
        {
            PackageId = _state.Id,
            Timestamp = DateTime.UtcNow,
            CartId = cartId,
            Version = _state.NextVersion()
        };
        _state.Apply(e);
        _events.Add(e);
    }
    public void BeginDelivery()
    {
        var e = new PackageOutForDelivery
        {
            PackageId = _state.Id,
            Timestamp = DateTime.UtcNow,
            Version = _state.NextVersion()
        };
        _state.Apply(e);
        _events.Add(e);
    }

}