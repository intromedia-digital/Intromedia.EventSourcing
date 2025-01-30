using System.Reflection;
using System.Text.Json.Serialization;
public class Event
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public IEventData Data { get; set; }
    public int Version { get; set; }
    public Event(Guid id, int version, IEventData data)
    {
        Id = id;
        Version = version;
        Type = data.GetType().GetCustomAttribute<EventNameAttribute>()?.EventName ?? data.GetType().Name;
        Data = data;
    }
    [JsonConstructor]
    protected Event()
    {
    }
}

