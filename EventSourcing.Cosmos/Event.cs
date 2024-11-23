
using System.Text.Json.Serialization;

internal sealed class Event
{
    public Guid StreamId { get; set; }
    public Guid Id { get; set; }
    public IEvent Data { get; set; }
    public Event(Guid streamId, IEvent @event) 
    {
        StreamId = streamId;
        Id = @event.EventId;
        Data = @event;
    }
    [JsonConstructor]
    private Event() { }
}

