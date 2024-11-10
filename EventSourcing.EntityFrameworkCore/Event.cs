using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

internal sealed class Event
{
    public long Id { get; set; }
    public Guid StreamId { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; }
    public string Payload { get; set; }
    public DateTime? Published { get; set; }
    public Event(Guid streamId, IEvent @event)
    {
        StreamId = streamId;
        Timestamp = DateTime.UtcNow;
        Version = @event.Version;
        Payload = JsonConvert.SerializeObject(@event, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
    }
    private Event() { }
}

