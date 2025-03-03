﻿using Azure.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace EventSourcing.Cosmos;

internal sealed class CosmosEventSourcingBuilder : ICosmosEventSourcingBuilder
{
    internal const string LeaseContainerId = "lease";
    public IServiceCollection Services { get; }
    public object ServiceKey { get; private set; }
    private bool ShouldUseKeyedServices { get; set; }
    private string DatabaseId { get; set; } = "eventsourcing";
    private List<string> ContainerIds { get; } = [];
    public CosmosEventSourcingBuilder(IServiceCollection services, object serviceKey, bool shouldUseKeyedServices)
    {
        Services = services;
        ServiceKey = serviceKey;
        ShouldUseKeyedServices = shouldUseKeyedServices;
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
    public void AddProjection<TProjection>(string streamType, DateTime? startFrom = null) where TProjection : Projection
    {
        Services.AddKeyedSingleton<TProjection>(ServiceKey);
        Services.AddSingleton<IHostedService>(sp =>
            new ProjectionChangeFeedProcessor<TProjection>(
                sp.GetRequiredService<ILogger<ProjectionChangeFeedProcessor<TProjection>>>(),
                sp.GetRequiredKeyedService<CosmosClient>(ServiceKey).GetContainer(DatabaseId, streamType),
                sp.GetRequiredKeyedService<CosmosClient>(ServiceKey).GetContainer(DatabaseId, LeaseContainerId),
                sp.GetRequiredKeyedService<TProjection>(ServiceKey),
                startFrom ?? DateTime.UtcNow
                )
            );
    }
    public void AddInMemoryPublisher()
    {
        foreach (string containerId in ContainerIds)
        {
            AddProjection<InMemoryEventPublisher>(containerId, DateTime.UtcNow);
        }
    }
}
