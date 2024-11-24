using Azure.Core;
using EventSourcing.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

public static class DependencyInjection
{
    public static IEventSourcingCosmosBuilder AddEventTypesFromAssemblies(this IEventSourcingCosmosBuilder builder, params Assembly[] assemblies)
    {
        JsonDerivedType[] eventTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t =>
            {
                EventNameAttribute attribute = t.GetCustomAttribute<EventNameAttribute>() ?? throw new EventNameAttributeNotSet(t);
                return new JsonDerivedType(t, attribute.EventName);
            })
            .ToArray() ?? [];

        builder.Services.AddKeyedSingleton<PolymorphicTypeResolver>(serviceKey: builder.ServiceKey, (sp, k) => new PolymorphicTypeResolver(eventTypes));
        return builder;
    }
    private static IEventSourcingCosmosBuilder AddOptionsAndInitializer(this IEventSourcingCosmosBuilder builder,
        string databaseId)
    {
        builder.Services.AddKeyedSingleton<CosmosDatabaseOptions>(serviceKey: builder.ServiceKey, (sp, key) => new CosmosDatabaseOptions
        {
            DatabaseId = databaseId
        });
        builder.Services.AddHostedService<Initializer>(sp => new Initializer(builder.ServiceKey, sp.GetRequiredService<IServiceScopeFactory>()));
        return builder;
    }
    public static IEventSourcingCosmosBuilder UseCosmos(this IEventSourcingBuilder builder,
        Uri endpoint,
        TokenCredential tokenCredential,
        string databaseId,
        object? serviceKey = null)
    {
        if (serviceKey is null)
        {
            serviceKey = Guid.NewGuid();
        }
        
        var cosmosBuilder = new EventSourcingCosmosBuilder(builder.Services, serviceKey);

        cosmosBuilder.Services.AddKeyedSingleton(serviceKey: serviceKey, (sp, key) => new CosmosClient(
           accountEndpoint: endpoint.AbsoluteUri,
            tokenCredential,
           new CosmosClientOptions
           {
               UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
               {
                   PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                   PropertyNameCaseInsensitive = true,
                   WriteIndented = true,
                   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                   DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                   TypeInfoResolver = sp.GetRequiredKeyedService<PolymorphicTypeResolver>(serviceKey)
               }
           }
       ));

        cosmosBuilder.AddOptionsAndInitializer(databaseId);

        return cosmosBuilder;
    }
    public static IEventSourcingCosmosBuilder UseCosmos(this IEventSourcingBuilder builder,
        string connectionString,
        string databaseId,
        object? serviceKey = null)
    {
        if (serviceKey is null)
        {
            serviceKey = Guid.NewGuid();
        }

        var cosmosBuilder = new EventSourcingCosmosBuilder(builder.Services, serviceKey);

        cosmosBuilder.Services.AddKeyedSingleton(serviceKey: serviceKey, (sp, key) => new CosmosClient(
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
                    TypeInfoResolver = sp.GetRequiredKeyedService<PolymorphicTypeResolver>(serviceKey)
                }
            }
        ));

        cosmosBuilder.AddOptionsAndInitializer(databaseId);

        return cosmosBuilder;
    }
   
    public static IEventSourcingCosmosBuilder AddAppendStream<TStream>(this IEventSourcingCosmosBuilder builder)
        where TStream : IStream
    {
        builder.Services.AddKeyedSingleton(typeof(TStream), serviceKey: builder.ServiceKey);
        builder.Services.AddKeyedSingleton<IStream>(serviceKey: builder.ServiceKey, (sp, k) => sp.GetRequiredKeyedService<TStream>(serviceKey: builder.ServiceKey));
        builder.Services.AddSingleton<IAppendStream<TStream>, EventStreams<TStream>>(sp =>
            new EventStreams<TStream>(
                builder.ServiceKey,
                sp.GetRequiredKeyedService<CosmosClient>(serviceKey: builder.ServiceKey),
                sp.GetRequiredKeyedService<CosmosDatabaseOptions>(serviceKey: builder.ServiceKey),
                sp.GetRequiredService<IServiceScopeFactory>()
                )
            );
        return builder;
    }
    public static IEventSourcingCosmosBuilder AddSubscription<TSubscription, TStream>(this IEventSourcingCosmosBuilder builder)
        where TSubscription : Subscription<TStream>
        where TStream : IStream
    {
        builder.Services.AddKeyedSingleton(typeof(TSubscription), serviceKey: builder.ServiceKey);
        builder.Services.AddHostedService<SubscriptionReader<TSubscription, TStream>>(sp => new SubscriptionReader<TSubscription, TStream>(builder.ServiceKey, sp.GetRequiredService<IServiceScopeFactory>()));
        return builder;
    }

}

