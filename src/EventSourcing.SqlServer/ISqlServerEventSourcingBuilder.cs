
namespace EventSourcing.SqlServer;
public interface ISqlServerEventSourcingBuilder 
{
    void AddProjection<T>(string streamType, DateTime? startFrom = null) where T : Projection;
    void AddInMemoryPublisher();
    void ConfigureInfrastructure();
    void UseConnectionString(string connectionString);
}
