internal sealed class PackageLoadedOnCart : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public required Guid PackageId { get; set; }
    public required Guid CartId { get; set; }
    public required int Version { get; set; }
}
