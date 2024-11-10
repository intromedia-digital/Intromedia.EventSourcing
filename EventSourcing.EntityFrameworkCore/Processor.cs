using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

internal sealed class Processor<TContext>(IServiceScopeFactory scopeFactory) : BackgroundService
    where TContext : DbContext
{
    private async Task HandleChanges(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IMediator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Processor<TContext>>>();
        using var db = scope.ServiceProvider.GetRequiredService<TContext>();

        var changes = await db.Set<Event>()
            .AsNoTracking()
            .Where(c => c.Published == null)
            .OrderBy(c => c.Timestamp)
            .ToListAsync(ct);

        foreach (var change in changes)
        {
            var @event = JsonConvert.DeserializeObject(change.Payload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
            if(@event is IEvent)
            {
                try
                {
                    await publisher.Publish(@event, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error handling event {@Event}", @event);
                }
            }
            else
                logger.LogWarning("Could not deserialize event in {@Change}", change);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            await HandleChanges(stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
