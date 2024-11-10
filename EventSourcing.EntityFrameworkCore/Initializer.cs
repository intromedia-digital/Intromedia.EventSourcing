using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal sealed class Initializer<TContext>(IServiceScopeFactory serviceScopeFactory) : IHostedService
    where TContext : DbContext
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<TContext>();
        return db.Database.MigrateAsync(cancellationToken);
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

