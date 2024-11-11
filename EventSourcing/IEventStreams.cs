public interface IEventStreams
{
    Task Append(Guid streamId, string streamType, params IEvent[] events);
    Task<TState> BuildState<TState>(Guid streamId) where TState : IState, new();
    Task<ReadStream> Get(string streamType, int offset);
}

public record ReadStream(int Offset, IReadOnlyList<IEvent> Events);