internal sealed class CosmosDatabaseOptions
{
    public string DatabaseId { get; set; }
    public string StreamContainerId { get; set; } = "streams";
    public string StreamLeaseContainerId => $"{StreamContainerId}_leases";
    public string SnapshotContainerId { get; set; } = "snapshots";
}