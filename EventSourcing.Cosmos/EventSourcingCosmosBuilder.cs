using Microsoft.Extensions.DependencyInjection;

internal class EventSourcingCosmosBuilder : IEventSourcingCosmosBuilder
{
    public IServiceCollection Services { get; }
    public EventSourcingCosmosBuilder(IServiceCollection services)
    {
        Services = services;
    }
}