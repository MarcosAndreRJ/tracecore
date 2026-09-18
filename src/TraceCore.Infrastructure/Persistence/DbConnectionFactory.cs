using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace TraceCore.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateConnectionAsync(CancellationToken ct = default);
}

public class MySqlDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlDbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TraceCoreDb")
            ?? "Server=localhost;Port=3306;Database=TraceCoreDb;User=root;Password=password;Connection Timeout=30;";
    }

    public async Task<DbConnection> CreateConnectionAsync(CancellationToken ct = default)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
