using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FinanceTracker.Infrastructure.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
        : this(configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not configured."))
    {
    }

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
