using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Cosmos;
internal sealed class ProjectionChangeFeedProcessor<TProjection>(
ILogger<ProjectionChangeFeedProcessor<TProjection>> logger,
Container container,
Container leaseContainer,
TProjection projection,
DateTime startFrom
)
: IHostedService
where TProjection : Projection
{
    private readonly ChangeFeedProcessor _processor = container.GetChangeFeedProcessorBuilder<IEvent>(projection.Name, (changes, ct) => Handle(logger, projection, changes, ct))
            .WithInstanceName(Environment.MachineName)
            .WithLeaseContainer(leaseContainer)
            .WithMaxItems(1)
            .WithErrorNotification((lt, ex) => HandleError(logger, lt, ex))
            .WithLeaseAcquireNotification(lt => HandleLeaseAquired(logger, lt))
            .WithLeaseReleaseNotification(lt => HandleLeaseReleased(logger, lt))
            .WithStartTime(startFrom.ToUniversalTime())
            .Build();
    private static async Task HandleLeaseReleased(ILogger<ProjectionChangeFeedProcessor<TProjection>> logger, string leaseToken)
    {
        logger.LogInformation("Released lease {LeaseToken} for {Processor}", leaseToken, typeof(TProjection).Name);
        await Task.CompletedTask;
    }
    private static async Task HandleLeaseAquired(ILogger<ProjectionChangeFeedProcessor<TProjection>> logger, string leaseToken)
    {
        logger.LogInformation("Acquired lease {LeaseToken} for {Processor}", leaseToken, typeof(TProjection).Name);
        await Task.CompletedTask;
    }
    private static async Task HandleError(ILogger<ProjectionChangeFeedProcessor<TProjection>> logger, string leaseToken, Exception exception)
    {
        logger.LogError(exception, "Error processing change feed for {Processor}, lease token {LeaseToken}", typeof(TProjection).Name, leaseToken);
        await Task.CompletedTask;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _processor.StartAsync();
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopAsync();
    }
    private async static Task Handle(ILogger<ProjectionChangeFeedProcessor<TProjection>> logger, TProjection projection, IReadOnlyCollection<object> changes, CancellationToken cancellationToken)
    {
        foreach (IEvent change in changes.OfType<IEvent>())
        {
            try
            {
                await projection.ProjectAsync(change, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error projecting change, {Processor} not continuing", projection.Name);
                throw;
            }
        }
    }
}
