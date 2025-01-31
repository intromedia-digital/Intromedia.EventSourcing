using Microsoft.Data.SqlClient;
using System.Data;

namespace EventSourcing.SqlServer;

internal sealed class DbConnectionFactory
{
    private readonly string _connectionString;
    public DbConnectionFactory(string connectionString) => _connectionString = connectionString;
    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
