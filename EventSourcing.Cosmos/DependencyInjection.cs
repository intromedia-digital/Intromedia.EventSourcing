using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
public static class DependencyInjection
{
   public static IEventSourcingBuilder AddEventTypesFromAssembies(this IEventSourcingBuilder builder, params Assembly[] assemblies)
    {
        var eventTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => new JsonDerivedType(t, t.Name))
            .ToArray();

        builder.Services.AddSingleton(sp => new PolymorphicTypeResolver(eventTypes));
        return builder;
    }
    public static IEventSourcingCosmosBuilder UseCosmos(this IEventSourcingBuilder builder,
        string connectionString,
        string databaseId,
        params Assembly[] eventAssemblies
        )
    {
        builder.Services.AddSingleton(sp => new CosmosClient(
            connectionString,
            new CosmosClientOptions
            {
                UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    TypeInfoResolver = sp.GetRequiredService<PolymorphicTypeResolver>()
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

