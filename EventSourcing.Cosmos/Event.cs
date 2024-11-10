using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

internal sealed class Event
{
    public Guid StreamId { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Payload { get; set; }
    public Event(Guid streamId, IEvent @event) 
    {
        StreamId = streamId;
        Timestamp = DateTime.UtcNow;
        Id = Guid.NewGuid();
        Version = @event.Version;
        Payload = JsonConvert.SerializeObject(@event, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
    }
    [JsonConstructor]
    private Event() { }
}

