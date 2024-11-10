internal sealed class PackageLoadedOnCart : IEvent
{
    public required Guid PackageId { get; set; }
    public required Guid CartId { get; set; }
    public required int Version { get; set; }
}
