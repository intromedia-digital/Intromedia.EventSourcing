public static class DependencyInjection
{

    public static void UseCosmos(this IEventSourcingBuilder eventSourcingBuilder, Action<IEventSourcingCosmosBuilder> configureCosmos)
    {
        var builder = new EventSourcingCosmosBuilder(eventSourcingBuilder.Services, eventSourcingBuilder.ServiceKey, eventSourcingBuilder.ShouldUseKeyedServices);
        configureCosmos(builder);
    }

}

