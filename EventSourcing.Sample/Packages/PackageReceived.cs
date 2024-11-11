internal sealed class PackageReceived : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public required Guid PackageId { get; set; }
    public required string TrackingNumber { get; set; }
    public required int Version { get; set; } = 0;
}

