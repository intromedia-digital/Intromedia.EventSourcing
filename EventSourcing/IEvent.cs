
public interface IEvent
{
    DateTime Timestamp { get; }
    Guid EventId { get; }
    int Version { get; }
}


