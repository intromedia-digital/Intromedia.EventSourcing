public interface IEventStreams
{
    Task Append<TStream>(Guid streamId, params IEvent[] events) where TStream : IStream;
    Task<TState> BuildState<TStream, TState>(Guid streamId)
        where TStream : IStream
        where TState : IState<TStream>, new();
}

