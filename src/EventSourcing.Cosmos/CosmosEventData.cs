using System.Reflection;

namespace EventSourcing.Cosmos;

public sealed class CosmosEventData
{
    
    public string Type { get; set; }
    public string StreamType { get; set; }
    public Guid StreamId { get; set; }
    public Guid Id { get; set; }
    public int Version { get; set; }
    public DateTime Created { get; set; }
    public IEvent Data { get; set; }
    public static CosmosEventData FromEvent(IEvent e, Guid streamId, string streamType)
    {
        return new CosmosEventData
        {
            Type = e.GetType().GetCustomAttribute<EventNameAttribute>()?.EventName ?? throw new EventNameAttributeNotSet(e.GetType()),
            StreamType = streamType,
            StreamId = streamId,
            Id = e.Id,
            Version = e.Version,
            Created = DateTime.UtcNow,
            Data = e
        };
    }

}
