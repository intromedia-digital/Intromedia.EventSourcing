using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class DependencyInjection
{
    public static IEventSourcingCosmosBuilder UseCosmos(this IEventSourcingBuilder builder, string connectionString, string databaseId)
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
        });

        builder.Services.AddHostedService<Initializer>();


        return new EventSourcingCosmosBuilder(builder.Services);
    }
    public static IEventSourcingCosmosBuilder AddAppendStream(this IEventSourcingCosmosBuilder builder)
    {
        builder.Services.AddSingleton<IEventStreams, EventStreams>();
        return builder;
    }
    public static IEventSourcingCosmosBuilder AddStream<TStream>(this IEventSourcingCosmosBuilder builder)
        where TStream : IStream
    {
        builder.Services.RemoveAll(typeof(TStream));
        builder.Services.AddSingleton(typeof(TStream));
        builder.Services.AddSingleton(typeof(IStream), sp => sp.GetRequiredService<TStream>());

        return builder;
    }
    public static IEventSourcingCosmosBuilder AddSubscription<TSubscription, TStream>(this IEventSourcingCosmosBuilder builder)
        where TSubscription : Subscription<TStream>
        where TStream : IStream
    {
        builder.Services.RemoveAll(typeof(TStream));
        builder.Services.AddSingleton(typeof(TStream));
        builder.Services.AddSingleton(typeof(IStream), sp => sp.GetRequiredService<TStream>());

        builder.Services.AddSingleton<TSubscription>();
        builder.Services.AddHostedService<SubscriptionReader<TSubscription, TStream>>();
        return builder;
    }

}
