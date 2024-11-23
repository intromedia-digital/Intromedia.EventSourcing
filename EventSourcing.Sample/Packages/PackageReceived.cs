using System.Text.Json.Serialization;

[EventName("package_received_v1")]
internal sealed record PackageReceived : IEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid EventId { get; set; } = Guid.NewGuid();
    public required Guid PackageId { get; set; }
    public required string TrackingNumber { get; set; }
    public required int Version { get; set; } = 0;
    public required PackageRecievedMetadata Metadata { get; set; }
}


[JsonDerivedType(typeof(PackageReceivedMetadataOne), "PackageReceivedMetadataOne")]
[JsonDerivedType(typeof(PackageReceivedMetadataTwo), "PackageReceivedMetadataTwo")]
internal abstract record PackageRecievedMetadata();

internal sealed record PackageReceivedMetadataOne : PackageRecievedMetadata
{
    public string Carrier { get; set; } = "UPS";
    public string Origin { get; set; } = "Seattle, WA";
}

internal sealed record PackageReceivedMetadataTwo : PackageRecievedMetadata
{
    public string Destination { get; set; } = "Miami, FL";
    public DateTime EstimatedDelivery { get; set; } = DateTime.UtcNow.AddDays(3);
}