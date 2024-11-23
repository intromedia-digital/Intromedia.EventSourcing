using Microsoft.Extensions.DependencyInjection;

internal class EventSourcingCosmosBuilder : IEventSourcingCosmosBuilder
{
    public IServiceCollection Services { get; }
    public object ServiceKey { get; init; }
    public EventSourcingCosmosBuilder(IServiceCollection services, object serviceKey)
    {
        Services = services;
        ServiceKey = serviceKey;
    }
}