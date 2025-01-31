using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace EventSourcing.Cosmos;
internal sealed class InMemoryEventPublisher(IServiceProvider serviceProvider) : Projection(serviceProvider)
{
    private readonly ILogger<InMemoryEventPublisher> _logger = serviceProvider.GetRequiredService<ILogger<InMemoryEventPublisher>>();
    private readonly IPublisher _publisher = serviceProvider.GetRequiredService<IPublisher>();
    public override string Name => "InMemoryEventPublisher";
    public override async Task ApplyAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(@event);
        try
        {
            _logger.LogInformation("Publishing event");
            await _publisher.Publish(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event");
        }
    }
}
