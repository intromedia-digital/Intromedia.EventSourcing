
using Azure.Core;

public interface IEventSourcingCosmosBuilder 
{
    void AddStream(string containerId);
    void ConfigureInfrastructure();
    void RegisterPolymorphicTypesFromAssemblyContaining<T>();
    void UseConnectionString(string connectionString);
    void UseCredential(Uri uri, TokenCredential tokenCredential);
    void UseDatabase(string databaseId);
}
