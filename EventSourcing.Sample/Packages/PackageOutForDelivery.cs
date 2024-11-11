internal sealed class PackageOutForDelivery : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public required Guid PackageId { get; set; }
    public required DateTime Timestamp { get; set; }
    public required int Version { get; set; }


}
