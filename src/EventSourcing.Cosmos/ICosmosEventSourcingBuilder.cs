
using Azure.Core;
namespace EventSourcing.Cosmos;
public interface ICosmosEventSourcingBuilder 
{
    void AddProjection<T>(string streamType, DateTime? startFrom = null) where T : Projection;
    void AddInMemoryPublisher();
    void AddStream(string containerId);
    void ConfigureInfrastructure();
    void UseConnectionString(string connectionString);
    void UseCredential(Uri uri, TokenCredential tokenCredential);
    void UseDatabase(string databaseId);
}
