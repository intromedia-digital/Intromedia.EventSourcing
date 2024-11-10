using Microsoft.EntityFrameworkCore;

internal sealed class PackageProjection(IServiceScopeFactory serviceScopeFactory, IEventStreams eventStreams) : BackgroundService
{
    private const string STREAM_TYPE = "package";
    private const string NAME = "package_read_model";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int offset = await GetOffset();
        do
        {
            var readStream = await eventStreams.Get(STREAM_TYPE, offset);
            offset = readStream.Offset;
            foreach (var @event in readStream.Events)
            {
                await ReceiveEvent(@event, offset);
            }
            await Task.Delay(500);
        } while (!stoppingToken.IsCancellationRequested);
    }
    public async Task ReceiveEvent(IEvent @event, int offset)
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<PackageContext>();
        using var transaction = context.Database.BeginTransaction();

        switch (@event)
        {
            case PackageReceived packageReceived:
                await Apply(packageReceived, context);
                break;
            case PackageOutForDelivery packageOutForDelivery:
                await Apply(packageOutForDelivery, context);
                break;
            case PackageLoadedOnCart packageLoadedOnCart:
                await Apply(packageLoadedOnCart, context);
                break;
        }

        await UpdateOffset(offset);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    private async Task UpdateOffset(int offset)
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<PackageContext>();

        var projection = await context.Projections
            .Where(p => p.Name == NAME)
            .FirstOrDefaultAsync();

        if (projection == null)
        {
            projection = new Projection(NAME, STREAM_TYPE);
            context.Projections.Add(projection);
        }

        projection.Offset = offset;
        await context.SaveChangesAsync();
    }
    private async Task<int> GetOffset()
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<PackageContext>();

        var projection = await context.Projections
            .Where(p => p.Name == NAME)
            .FirstOrDefaultAsync();

        if (projection == null)
        {
            projection = new Projection(NAME, STREAM_TYPE);
            context.Projections.Add(projection);
            await context.SaveChangesAsync();
        }

        return projection.Offset;
    }
    private async Task Apply(PackageReceived @event, PackageContext context)
    {
        var readModel = new PackageReadModel
        {
            Id = @event.PackageId,
            TrackingNumber = @event.TrackingNumber,
            Version = @event.Version
        };
        await context.Packages.AddAsync(readModel);
    }
    private async Task Apply(PackageOutForDelivery @event, PackageContext context)
    {
        var readModel = await Get(@event.PackageId, context);
        readModel.OutForDelivery = @event.Timestamp;
        readModel.Version = @event.Version;
    }
    private async Task Apply(PackageLoadedOnCart @event, PackageContext context)
    {
        var readModel = await Get(@event.PackageId, context);
        readModel.CartId = @event.CartId;
        readModel.Version = @event.Version;
    }
    private async Task<PackageReadModel> Get(Guid packageId, PackageContext context)
    {
        var readModel = await context.Packages
            .Where(p => p.Id == packageId)
            .SingleOrDefaultAsync();

        if (readModel == null)
        {
            throw new InvalidOperationException("Package not found");
        }

        return readModel;
    }
   
}