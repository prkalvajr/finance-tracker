using System.Data.Common;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Infrastructure.Data;

namespace FinanceTracker.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(RefreshToken token, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO refresh_tokens (user_id, token, expires_at, revoked_at)
            VALUES (@user_id, @token, @expires_at, @revoked_at)
            RETURNING id;
            """;
        AddParameter(cmd, "user_id", token.UserId);
        AddParameter(cmd, "token", token.Token);
        AddParameter(cmd, "expires_at", token.ExpiresAt);
        AddParameter(cmd, "revoked_at", (object?)token.RevokedAt ?? DBNull.Value);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        token.Id = id;
        return id;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, user_id, token, expires_at, revoked_at
              FROM refresh_tokens
             WHERE token = @token;
            """;
        AddParameter(cmd, "token", token);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new RefreshToken
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            Token = reader.GetString(2),
            ExpiresAt = reader.GetDateTime(3),
            RevokedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
        };
    }

    public async Task RevokeAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE refresh_tokens
               SET revoked_at = NOW() AT TIME ZONE 'UTC'
             WHERE id = @id
               AND revoked_at IS NULL;
            """;
        AddParameter(cmd, "id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void AddParameter(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
