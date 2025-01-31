namespace EventSourcing.Cosmos;

public abstract class Projection /*: IProjection*/
{
    protected readonly IServiceProvider ServiceProvider;
    public Projection(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
    public abstract string Name { get; }
    public abstract Task ApplyAsync(IEvent @event, CancellationToken cancellationToken = default);
}