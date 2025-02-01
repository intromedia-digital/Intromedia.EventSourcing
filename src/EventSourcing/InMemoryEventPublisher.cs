using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace EventSourcing;
public sealed class InMemoryEventPublisher(IServiceProvider serviceProvider) : Projection(serviceProvider)
{
    private readonly ILogger<InMemoryEventPublisher> _logger = serviceProvider.GetRequiredService<ILogger<InMemoryEventPublisher>>();
    private readonly IPublisher _publisher = serviceProvider.GetRequiredService<IPublisher>();
    public override string Name => "InMemoryEventPublisher";
    public override async Task ApplyAsync(Guid streamId, IEvent @event, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(@event);
        try
        {
            _logger.LogInformation("Publishing event");

            var eventType = @event.GetType();
            var eventContextType = typeof(EventContext<>).MakeGenericType(eventType);
            var eventContext = Activator.CreateInstance(eventContextType, @event, streamId);
            await _publisher.Publish(eventContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event");
        }
    }

}
