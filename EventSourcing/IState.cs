
public interface IState<TStream> where TStream : IStream
{
    Guid Id { get; set; }
    int Version { get; set; }
    int NextVersion();
    void Apply(IEvent @event);
}

