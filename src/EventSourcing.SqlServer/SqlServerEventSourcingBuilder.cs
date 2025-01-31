using EventSourcing.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace EventSourcing.SqlServer;

internal sealed class SqlServerEventSourcingBuilder : ISqlServerEventSourcingBuilder
{
    public IServiceCollection Services { get; }
    public object ServiceKey { get; private set; }
    private bool ShouldUseKeyedServices { get; set; }
    private string DatabaseName { get; set; } = "EventSourcing";
    public SqlServerEventSourcingBuilder(IServiceCollection services, object serviceKey, bool shouldUseKeyedServices)
    {
        Services = services;
        ServiceKey = serviceKey;
        ShouldUseKeyedServices = shouldUseKeyedServices;

        Services.AddSingleton(sp => new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = sp.GetRequiredKeyedService<PolymorphicTypeResolver>(ServiceKey)
        });
    }

    public void UseConnectionString(string connectionString)
    {
        Services.AddKeyedSingleton<DbConnectionFactory>(ServiceKey, (sp, _) => new DbConnectionFactory(connectionString));
        if (!ShouldUseKeyedServices)
        {
            Services.AddSingleton(sp => sp.GetRequiredKeyedService<DbConnectionFactory>(ServiceKey));
        }

        var dbName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (!string.IsNullOrWhiteSpace(dbName))
        {
            DatabaseName = dbName;
        }
    }
    public void ConfigureInfrastructure()
    {
        Services.AddKeyedSingleton<SqlConfigureDatabase>(ServiceKey, (sp, _) => new SqlConfigureDatabase(sp.GetRequiredKeyedService<DbConnectionFactory>(ServiceKey), DatabaseName));
        Services.AddSingleton<IHostedService, SqlConfigureDatabase>(sp => sp.GetRequiredKeyedService<SqlConfigureDatabase>(ServiceKey));
        Services.AddKeyedSingleton<IEventStore, SqlServerEventStore>(ServiceKey);

        if (!ShouldUseKeyedServices)
        {
            Services.AddSingleton<IEventStore>(sp => sp.GetRequiredKeyedService<IEventStore>(ServiceKey));
        }

    }
    
      public void AddProjection<TProjection>(string streamType, DateTime? startFrom = null) where TProjection : Projection
    {
        Services.AddKeyedSingleton<TProjection>(ServiceKey);
        Services.AddSingleton<IHostedService>(sp =>
            new ProjectionPollingProcessor<TProjection>(
                sp.GetRequiredService<ILogger<ProjectionPollingProcessor<TProjection>>>(),
                sp.GetRequiredKeyedService<DbConnectionFactory>(ServiceKey),
                streamType,
                sp.GetRequiredKeyedService<TProjection>(ServiceKey),
                startFrom ?? DateTime.UtcNow,
                sp.GetRequiredService<JsonSerializerOptions>()
                )
            );
    }

    public void AddInMemoryPublisher()
    {
        Services.AddKeyedSingleton<InMemoryEventPublisher>(ServiceKey);
        Services.AddSingleton<IHostedService>(sp =>
            new InMemoryEventPublisherPollingProcessor<InMemoryEventPublisher>(
                sp.GetRequiredService<ILogger<InMemoryEventPublisherPollingProcessor<InMemoryEventPublisher>>>(),
                sp.GetRequiredKeyedService<DbConnectionFactory>(ServiceKey),
                sp.GetRequiredKeyedService<InMemoryEventPublisher>(ServiceKey),
                sp.GetRequiredService<JsonSerializerOptions>()
                )
            );
    }

   
}
