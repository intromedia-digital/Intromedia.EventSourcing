public abstract class Subscription<TStream>
    where TStream : IStream
{
    private readonly List<Type> _subscribedEvents = new();
    public DateTime StartTime { get; private set; } = DateTime.MinValue;
    protected void Subscribe<TEvent>() where TEvent : IEvent
    {
        _subscribedEvents.Add(typeof(TEvent));
    }
    protected void StartFrom(DateTime startTime)
    {
        StartTime = startTime;
    }
    public IReadOnlyList<Type> SubscribedEvents => _subscribedEvents.AsReadOnly();
    public abstract Task HandleEvent(Guid streamId, IEvent @event, CancellationToken cancellationToken);
}