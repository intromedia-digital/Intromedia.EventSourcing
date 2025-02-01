namespace EventSourcing;

public abstract class Projection 
{
    protected readonly IServiceProvider ServiceProvider;
    public Projection(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
    public abstract string Name { get; }
    public abstract Task ApplyAsync(Guid streamId, IEvent @event, CancellationToken cancellationToken = default);
}