using Microsoft.Azure.Cosmos;

internal sealed class PackageRepository(IEventStreams eventStreams)
{
    public async Task<PackageAggregate> Get(Guid packageId)
    {
        var state = await eventStreams.BuildState<PackageStream, PackageState>(packageId);
        if (state is null)
        {
            throw new InvalidOperationException("Package not found");
        }
        return new PackageAggregate(state);
    }
    public async Task Save(PackageAggregate aggregate)
    {
        var events = aggregate.PopEvents();
        await eventStreams.Append<PackageStream>(aggregate.Id, events.ToArray());
    }
}