using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class DependencyInjection
{
    public static IEventSourcingCosmosBuilder UseCosmos(this IEventSourcingBuilder builder, string connectionString, string databaseId, string streamContainerId = "streams")
    {
        builder.Services.AddSingleton(new CosmosClient(
            connectionString,
            new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            }
        ));

        builder.Services.AddOptions<CosmosDatabaseOptions>().Configure(options =>
        {
            options.DatabaseId = databaseId;
            options.StreamContainerId = streamContainerId;
        });

        builder.Services.AddHostedService<Initializer>();

        builder.Services.AddHostedService<Processor>();


        builder.Services.AddMediatR(c => {
            c.RegisterServicesFromAssembly(assembly: typeof(DependencyInjection).Assembly);
        });

        return new EventSourcingCosmosBuilder(builder.Services);
    }
    public static IEventSourcingCosmosBuilder AddStreams(this IEventSourcingCosmosBuilder builder)
    {
        builder.Services.AddSingleton<IEventStreams, EventStreams>();
        return builder;
    }
    public static IEventSourcingCosmosBuilder AddSubscription<TSubscription>(this IEventSourcingCosmosBuilder builder)
        where TSubscription : Subscription
    {
        builder.Services.AddSingleton<TSubscription>();
        builder.Services.AddHostedService<SubscriptionReader<TSubscription>>();
        return builder;
    }

}


public interface IEventSourcingCosmosBuilder : IEventSourcingBuilder
{
}

internal class EventSourcingCosmosBuilder : IEventSourcingCosmosBuilder
{
    public IServiceCollection Services { get; }
    public EventSourcingCosmosBuilder(IServiceCollection services)
    {
        Services = services;
    }
}