using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing;

public class EventSourcingBuilder : IEventSourcingBuilder
{
    public EventSourcingBuilder(IServiceCollection services)
    {
        Services = services;
    }
    public object ServiceKey { get; private set; } = Guid.NewGuid();
    public bool ShouldUseKeyedServices { get; private set; } = false;
    public void UseKeyedServices(object serviceKey)
    {
        ServiceKey = serviceKey;
        ShouldUseKeyedServices = true;
    }
    public IServiceCollection Services { get; }
}

