using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Cosmos;
public static class DependencyInjection
{

    public static void UseCosmos(this IEventSourcingBuilder eventSourcingBuilder, Action<ICosmosEventSourcingBuilder> configureCosmos)
    {
        var builder = new CosmosEventSourcingBuilder(eventSourcingBuilder.Services, eventSourcingBuilder.ServiceKey, eventSourcingBuilder.ShouldUseKeyedServices);
        configureCosmos(builder);
    }

}

