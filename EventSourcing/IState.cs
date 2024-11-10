
public interface IState
{
    Guid Id { get; set; }
    int Version { get; set; }
    int NextVersion();
    void Apply(IEvent @event);
}

