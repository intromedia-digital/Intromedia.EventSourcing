using Microsoft.Azure.Cosmos;

internal sealed class PackageRepository(CosmosClient cosmos, IEventStreams eventStreams)
{
    private readonly Container states = cosmos.GetContainer("event-sourcing", "packages");
    public async Task<PackageAggregate> Get(Guid packageId)
    {
        var state = await eventStreams.BuildState<PackageState>(packageId);
        if (state is null)
        {
            throw new InvalidOperationException("Package not found");
        }
        return new PackageAggregate(state);
    }
    public async Task Save(PackageAggregate aggregate)
    {
        var events = aggregate.PopEvents();
        await eventStreams.Append(aggregate.Id, events.ToArray());
    }
}