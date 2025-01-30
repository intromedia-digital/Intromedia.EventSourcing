using Microsoft.Azure.Cosmos;

internal sealed class PackageRepository(IAppendStream<PackageStream> packageStream)
{
    public async Task<PackageAggregate> Get(Guid packageId)
    {
        var state = await packageStream.BuildState<PackageState>(packageId);

        if (state is null)
        {
            throw new InvalidOperationException("Package not found");
        }
        return new PackageAggregate(state);
    }
    public async Task Save(PackageAggregate aggregate)
    {
        var events = aggregate.PopEvents();
        await packageStream.Append(aggregate.Id, events.ToArray());
    }
}

