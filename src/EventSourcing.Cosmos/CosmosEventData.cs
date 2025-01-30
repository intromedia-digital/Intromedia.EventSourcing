using System.Text.Json.Serialization;

internal sealed class CosmosEventData : Event
{
    public Guid StreamId { get; set; }
    public CosmosEventData(Guid streamId, Event @event) : base(@event.Id, @event.Version, @event.Data)
    {
        StreamId = streamId;
    }
    [JsonConstructor]
    private CosmosEventData() : base()
    {
    }
}
