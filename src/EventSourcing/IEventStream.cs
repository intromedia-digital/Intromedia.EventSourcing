using System.Diagnostics.Tracing;
public interface IEventStream
{
    Task AppendToStreamAsync(IEnumerable<Event> events, CancellationToken cancellationToken = default);
    Task<IEnumerable<Event>> ReadStreamAsync(CancellationToken cancellationToken = default);
}

