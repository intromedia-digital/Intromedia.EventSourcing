public interface IEventStreams
{
    Task Append(Guid streamId, string streamType, params IEvent[] events);
    Task<TState> BuildState<TState>(Guid streamId) where TState : IState, new();
}

