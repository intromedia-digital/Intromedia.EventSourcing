using Microsoft.Azure.Cosmos;

internal sealed class PackageRepository(IAppendStream<PackageStream> packageStream, IAppendStream<Packge2Stream> package2Stream)
{
    public async Task<PackageAggregate> Get(Guid packageId)
    {
        var state = await packageStream.BuildState<PackageState>(packageId);
        var state2 = await package2Stream.BuildState<Package2State>(packageId);

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

