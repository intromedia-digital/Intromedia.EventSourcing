using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

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
    public void RegisterPolymorphicTypesFromAssemblyContaining<T>()
    {
        var assembly = typeof(T).Assembly;
        JsonDerivedType[] eventTypes = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEvent)) && !t.IsInterface && !t.IsAbstract)
            .Select(t =>
            {
                EventNameAttribute attribute = t.GetCustomAttribute<EventNameAttribute>() ?? throw new EventNameAttributeNotSet(t);
                return new JsonDerivedType(t, attribute.EventName);
            })
            .ToArray() ?? [];

        Services.AddKeyedSingleton<PolymorphicTypeResolver>(serviceKey: ServiceKey, (sp, k) => new PolymorphicTypeResolver(eventTypes));
    }
    public IServiceCollection Services { get; }

}

