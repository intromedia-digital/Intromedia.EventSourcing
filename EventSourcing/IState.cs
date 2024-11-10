
public interface IState
{
    Guid Id { get; set; }
    int Version { get; set; }
    void Apply(IEvent @event);
}
