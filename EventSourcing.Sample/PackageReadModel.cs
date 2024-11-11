internal sealed class PackageReadModel
{
    public Guid Id { get; set; }
    public string TrackingNumber { get; set; }
    public Guid? CartId { get; set; }
    public DateTime? OutForDelivery { get; set; }
    public int Version { get; set; } = 0;
}
