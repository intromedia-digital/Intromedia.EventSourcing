using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class DependencyInjection
{
    public static IEventSourcingBuilder UseCosmos(this IEventSourcingBuilder builder, string connectionString, string databaseId, string streamContainerId = "streams")
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

        builder.Services.RemoveAll<IEventStreams>();
        builder.Services.AddSingleton<IEventStreams, EventStreams>();

        builder.Services.AddMediatR(c => {
            c.RegisterServicesFromAssembly(assembly: typeof(DependencyInjection).Assembly);
        });

        return builder;
    }
}
