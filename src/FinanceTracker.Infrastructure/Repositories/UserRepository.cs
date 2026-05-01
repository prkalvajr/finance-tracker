using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Infrastructure.Data;

namespace FinanceTracker.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(User user, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO users (name, email, password_hash)
            VALUES (@name, @email, @password_hash)
            RETURNING user_id;
            """;
        AddParameter(cmd, "name", user.Name);
        AddParameter(cmd, "email", user.Email);
        AddParameter(cmd, "password_hash", user.PasswordHash);

        var result = await cmd.ExecuteScalarAsync(ct);
        var id = Convert.ToInt32(result);
        user.UserId = id;
        return id;
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT user_id, name, email, password_hash FROM users WHERE user_id = @id;";
        AddParameter(cmd, "id", userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapUser(reader) : null;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT user_id, name, email, password_hash FROM users WHERE email = @email;";
        AddParameter(cmd, "email", email);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapUser(reader) : null;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE users
               SET name = @name,
                   email = @email,
                   password_hash = @password_hash
             WHERE user_id = @id;
            """;
        AddParameter(cmd, "id", user.UserId);
        AddParameter(cmd, "name", user.Name);
        AddParameter(cmd, "email", user.Email);
        AddParameter(cmd, "password_hash", user.PasswordHash);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static User MapUser(System.Data.Common.DbDataReader reader) => new()
    {
        UserId = reader.GetInt32(0),
        Name = reader.GetString(1),
        Email = reader.GetString(2),
        PasswordHash = reader.GetString(3),
    };

    private static void AddParameter(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
