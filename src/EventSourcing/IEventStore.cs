public interface IEventStore
{
    Task<IEventStream> OpenStream(string type, Guid streamId, CancellationToken cancellationToken = default);
}

