
namespace EventSourcing.SqlServer;

public static class DependencyInjection
{
    public static void UseSqlServer(this IEventSourcingBuilder eventSourcingBuilder, Action<ISqlServerEventSourcingBuilder> configureOptions)
    {
        var builder = new SqlServerEventSourcingBuilder(eventSourcingBuilder.Services, eventSourcingBuilder.ServiceKey, eventSourcingBuilder.ShouldUseKeyedServices);
        configureOptions(builder);
    }
}

