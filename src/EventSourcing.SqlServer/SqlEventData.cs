using System.Reflection;
using System.Text.Json;

namespace EventSourcing.SqlServer;

internal sealed class SqlEventData
{
    
    public string Type { get; set; }
    public string StreamType { get; set; }
    public Guid StreamId { get; set; }
    public Guid Id { get; set; }
    public int Version { get; set; }
    public DateTime Created { get; set; }
    public string Data { get; set; }
    public IEvent ToEvent(JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<IEvent>(Data, options) ?? throw new InvalidOperationException("Failed to deserialize event");
    }
    public static SqlEventData FromEvent(IEvent e, string streamType, JsonSerializerOptions options)
    {
        return new SqlEventData
        {
            Type = e.GetType().GetCustomAttribute<EventNameAttribute>()?.EventName ?? throw new EventNameAttributeNotSet(e.GetType()),
            StreamType = streamType,
            StreamId = e.StreamId,
            Id = e.Id,
            Version = e.Version,
            Created = DateTime.UtcNow,
            Data = JsonSerializer.Serialize(e, options)
        };
    }

}
