public interface IEventStreams
{
    Task Append(Guid streamId, params IEvent[] events);
    Task<TState> BuildState<TState>(Guid streamId) where TState : IState, new();
}
