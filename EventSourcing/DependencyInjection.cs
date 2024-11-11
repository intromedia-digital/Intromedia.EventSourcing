using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IEventSourcingBuilder AddEventSourcing(this IServiceCollection services)
    {
        return new EventSourcingBuilder(services);
    }

  
}

