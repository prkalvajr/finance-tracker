using System.Data.Common;
using System.Reflection;
using FinanceTracker.Infrastructure.Data;
using Npgsql;

namespace FinanceTracker.Infrastructure.Migrations;

public static class MigrationRunner
{
    public static async Task ApplyAsync(IDbConnectionFactory connectionFactory, CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        foreach (var sql in LoadMigrationScripts())
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public static async Task DropAllAsync(IDbConnectionFactory connectionFactory, CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            DROP TABLE IF EXISTS refresh_tokens CASCADE;
            DROP TABLE IF EXISTS transactions CASCADE;
            DROP TABLE IF EXISTS users CASCADE;
            """;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public static async Task TruncateAllAsync(IDbConnectionFactory connectionFactory, CancellationToken ct = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "TRUNCATE TABLE refresh_tokens, transactions, users RESTART IDENTITY CASCADE;";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static IEnumerable<string> LoadMigrationScripts()
    {
        var assembly = typeof(MigrationRunner).Assembly;
        var resources = assembly
            .GetManifestResourceNames()
            .Where(n => n.Contains(".Migrations.") && n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        foreach (var resourceName in resources)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Migration resource '{resourceName}' not found.");
            using var reader = new StreamReader(stream);
            yield return reader.ReadToEnd();
        }
    }
}
