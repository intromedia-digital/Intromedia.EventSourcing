internal sealed class PackageOutForDelivery : IEvent
{
    public required Guid PackageId { get; set; }
    public required DateTime Timestamp { get; set; }
    public required int Version { get; set; }


}
