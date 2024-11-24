public interface IAppendStream<TStream>
	where TStream : IStream
{
	Task Append(Guid streamId, params IEvent[] events);
	Task<TState> BuildState<TState>(Guid streamId)
		where TState : IState<TStream>, new();
   

}