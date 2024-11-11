using Microsoft.EntityFrameworkCore;

internal sealed class PackageProjection(IServiceScopeFactory serviceScopeFactory)
{
    public async Task Apply(IEvent @event)
    {
        switch (@event)
        {
            case PackageReceived packageReceived:
                await Apply(packageReceived);
                break;
            case PackageOutForDelivery packageOutForDelivery:
                await Apply(packageOutForDelivery);
                break;
            case PackageLoadedOnCart packageLoadedOnCart:
                await Apply(packageLoadedOnCart);
                break;
        }
    }
    private async Task Apply(PackageReceived @event)
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<PackageContext>();
        var readModel = new PackageReadModel
        {
            Id = @event.PackageId,
            TrackingNumber = @event.TrackingNumber,
            Version = @event.Version
        };
        await context.Packages.AddAsync(readModel);
        await context.SaveChangesAsync();
    }
    private async Task Apply(PackageOutForDelivery @event)
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<PackageContext>();
        var readModel = await Get(@event.PackageId, context);
        readModel.OutForDelivery = @event.Timestamp;
        readModel.Version = @event.Version;
        await context.SaveChangesAsync();
    }
    private async Task Apply(PackageLoadedOnCart @event)
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<PackageContext>();
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