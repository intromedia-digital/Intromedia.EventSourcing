using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class DependencyInjection
{
    public static IEventSourcingBuilder AddEventSourcing(this IServiceCollection services)
    {
        return new EventSourcingBuilder(services);
    }

    public static IEventSourcingBuilder AddHandlersFromAssembly(this IEventSourcingBuilder builder, Assembly assembly)
    {
        // Get all implementations of IEventHandler<TEvent> and register them
        var handlerType = typeof(IEventHandler<>);
        var mediatrType = typeof(INotificationHandler<>);

        var handlers = assembly.GetTypes().ToList()
            .Where(p => p.GetInterfaces()
            .Any(i => 
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == mediatrType
            ));

        foreach (var t in handlers)
        {
            var interfaces = t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == mediatrType);

            foreach (var i in interfaces)
            {
                builder.Services.AddTransient(i, t);
            }
        }

        return builder;
    }

    public static IEventSourcingBuilder AddHandlersFromAssemblyContaining<TInAssembly>(this IEventSourcingBuilder builder) =>
        AddHandlersFromAssembly(builder, typeof(TInAssembly).Assembly);
}

public interface IEventHandler<TEvent> : INotificationHandler<TEvent>
    where TEvent : IEvent
{
}
public interface IEventSourcingBuilder
{
    IServiceCollection Services { get; }
}

public class EventSourcingBuilder : IEventSourcingBuilder
{
    public EventSourcingBuilder(IServiceCollection services)
    {
        Services = services;
    }
    public IServiceCollection Services { get; }
}

