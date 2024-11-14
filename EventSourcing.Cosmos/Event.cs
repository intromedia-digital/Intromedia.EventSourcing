using Newtonsoft.Json;

internal sealed class Event
{
    public Guid StreamId { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }
    public Guid Id { get; set; }
    public int Version { get; set; }
    public IEvent Data { get; set; }
    public Event(Guid streamId, IEvent @event) 
    {
        StreamId = streamId;
        Timestamp = DateTime.UtcNow;
        Id = Guid.NewGuid();
        Version = @event.Version;
        EventType = @event.GetType().Name;
        Data = @event;
    }
    [JsonConstructor]
    private Event() { }
}

