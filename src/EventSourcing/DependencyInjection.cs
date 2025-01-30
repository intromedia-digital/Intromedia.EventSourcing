using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddEventSourcing(this IServiceCollection services, Action<IEventSourcingBuilder> configureOptions)
    {
        var builder = new EventSourcingBuilder(services);
        configureOptions(builder);
    }
}

