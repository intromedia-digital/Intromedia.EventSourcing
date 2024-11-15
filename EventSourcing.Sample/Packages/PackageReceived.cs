internal sealed record PackageReceived : IEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid EventId { get; set; } = Guid.NewGuid();
    public required Guid PackageId { get; set; }
    public required string TrackingNumber { get; set; }
    public required int Version { get; set; } = 0;
}

