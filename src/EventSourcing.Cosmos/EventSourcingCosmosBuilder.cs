using Azure.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

internal sealed class EventSourcingCosmosBuilder : IEventSourcingCosmosBuilder
{
    public IServiceCollection Services { get; }
    public object ServiceKey { get; private set; }
    private bool ShouldUseKeyedServices { get; set; }
    private string DatabaseId { get; set; } = "eventsourcing";
    private List<string> ContainerIds { get; } = new();
    public EventSourcingCosmosBuilder(IServiceCollection services, object serviceKey, bool shouldUseKeyedServices)
    {
        Services = services;
        ServiceKey = serviceKey;
        ShouldUseKeyedServices = shouldUseKeyedServices;
    }
    public void RegisterPolymorphicTypesFromAssemblyContaining<T>()
    {
        var assembly = typeof(T).Assembly;
        JsonDerivedType[] eventTypes = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEventData)) && !t.IsInterface && !t.IsAbstract)
            .Select(t =>
            {
                EventNameAttribute attribute = t.GetCustomAttribute<EventNameAttribute>() ?? throw new EventNameAttributeNotSet(t);
                return new JsonDerivedType(t, attribute.EventName);
            })
            .ToArray() ?? [];

        Services.AddKeyedSingleton<PolymorphicTypeResolver>(serviceKey: ServiceKey, (sp, k) => new PolymorphicTypeResolver(eventTypes));
    }
    public void UseConnectionString(string connectionString)
    {
        Services.AddKeyedSingleton(ServiceKey, (sp, _) => new CosmosClient(connectionString,
           new CosmosClientOptions
           {
               UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
               {
                   PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                   PropertyNameCaseInsensitive = true,
                   WriteIndented = true,
                   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                   DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                   TypeInfoResolver = sp.GetRequiredKeyedService<PolymorphicTypeResolver>(ServiceKey)
               }
           }));
        if (!ShouldUseKeyedServices)
        {
            Services.AddSingleton(sp => sp.GetRequiredKeyedService<CosmosClient>(ServiceKey));
        }
    }
    public void UseCredential(Uri uri, TokenCredential tokenCredential)
    {
        Services.AddKeyedSingleton(ServiceKey, (sp, _) => new CosmosClient(uri.AbsoluteUri, tokenCredential,
           new CosmosClientOptions
           {
               UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
               {
                   PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                   PropertyNameCaseInsensitive = true,
                   WriteIndented = true,
                   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                   DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                   TypeInfoResolver = sp.GetRequiredKeyedService<PolymorphicTypeResolver>(ServiceKey)
               }
           }));
        if (!ShouldUseKeyedServices)
        {
            Services.AddSingleton(sp => sp.GetRequiredKeyedService<CosmosClient>(ServiceKey));
        }
    }
    public void UseDatabase(string databaseId)
    {
        DatabaseId = databaseId;
    }
    public void AddStream(string containerId)
    {
        ContainerIds.Add(containerId);
    }
    public void ConfigureInfrastructure()
    {
        Services.AddKeyedSingleton<CosmosInfrastructureInitializer>(ServiceKey, (sp, _) => new CosmosInfrastructureInitializer(sp.GetRequiredKeyedService<CosmosClient>(ServiceKey), DatabaseId, ContainerIds));
        Services.AddSingleton<IHostedService, CosmosInfrastructureInitializer>(sp => sp.GetRequiredKeyedService<CosmosInfrastructureInitializer>(ServiceKey));
        Services.AddKeyedSingleton<IEventStore>(ServiceKey, (sp, _) => new CosmosEventStore(sp.GetRequiredKeyedService<CosmosClient>(ServiceKey), DatabaseId));

        if (!ShouldUseKeyedServices)
        {
            Services.AddSingleton(sp => sp.GetRequiredKeyedService<CosmosInfrastructureInitializer>(ServiceKey));
            Services.AddSingleton<IEventStore>(sp => sp.GetRequiredKeyedService<IEventStore>(ServiceKey));
        }
    }
}
