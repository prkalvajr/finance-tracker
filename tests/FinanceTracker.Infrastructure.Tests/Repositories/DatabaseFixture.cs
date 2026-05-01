using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Migrations;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Infrastructure.Tests.Repositories;

public class DatabaseFixture : IAsyncLifetime
{
    public DbConnectionFactory ConnectionFactory { get; }

    public DatabaseFixture()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default not configured.");

        ConnectionFactory = new DbConnectionFactory(connectionString);
    }

    public async Task InitializeAsync()
    {
        await MigrationRunner.DropAllAsync(ConnectionFactory);
        await MigrationRunner.ApplyAsync(ConnectionFactory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public Task ResetAsync() => MigrationRunner.TruncateAllAsync(ConnectionFactory);
}
