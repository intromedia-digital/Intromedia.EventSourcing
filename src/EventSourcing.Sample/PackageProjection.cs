using Microsoft.EntityFrameworkCore;
internal sealed class PackageProjection(IServiceScopeFactory serviceScopeFactory)
{
    public async Task Apply(IEvent @event)
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<PackageContext>();

        var existingEvent = await context.ReceivedEvents
            .AnyAsync(e => e.EventId == @event.EventId);

        if (existingEvent)
        {
            return;
        }

        var transaction = await context.Database.BeginTransactionAsync();

        var receivedEvent = new ReceivedEvents
        {
            EventId = @event.EventId
        };

        await context.ReceivedEvents.AddAsync(receivedEvent);
        await context.SaveChangesAsync();

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

        try
        {
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
        }
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
        await context.SaveChangesAsync();
    }
    private async Task Apply(PackageOutForDelivery @event, PackageContext context)
    {
        var readModel = await Get(@event.PackageId, context);
        readModel.OutForDelivery = @event.Timestamp;
        readModel.Version = @event.Version;
        await context.SaveChangesAsync();
    }
    private async Task Apply(PackageLoadedOnCart @event, PackageContext context)
    {
        var readModel = await Get(@event.PackageId, context);
        readModel.CartId = @event.CartId;
        readModel.Version = @event.Version;
        await context.SaveChangesAsync();
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