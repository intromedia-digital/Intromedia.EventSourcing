namespace EventSourcing;
public interface IEventStore
{
    Task AppendToStreamAsync(string type, Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
    Task<IEnumerable<IEvent>> ReadStreamAsync(string type, Guid streamId, CancellationToken cancellationToken = default);
}
