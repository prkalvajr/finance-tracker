using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Repositories;
using FluentAssertions;

namespace FinanceTracker.Infrastructure.Tests.Repositories;

[Collection("Database")]
public class UserRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly UserRepository _repository;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new UserRepository(_fixture.ConnectionFactory);
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAsync_InsertsUserAndReturnsGeneratedId()
    {
        var user = new User { Name = "Alice", Email = "alice@example.com", PasswordHash = "hash" };

        var id = await _repository.CreateAsync(user);

        id.Should().BeGreaterThan(0);
        user.UserId.Should().Be(id);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsUser()
    {
        var user = new User { Name = "Bob", Email = "bob@example.com", PasswordHash = "hash" };
        var id = await _repository.CreateAsync(user);

        var found = await _repository.GetByIdAsync(id);

        found.Should().NotBeNull();
        found!.UserId.Should().Be(id);
        found.Name.Should().Be("Bob");
        found.Email.Should().Be("bob@example.com");
        found.PasswordHash.Should().Be("hash");
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var found = await _repository.GetByIdAsync(999_999);

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsUser()
    {
        var user = new User { Name = "Carol", Email = "carol@example.com", PasswordHash = "hash" };
        await _repository.CreateAsync(user);

        var found = await _repository.GetByEmailAsync("carol@example.com");

        found.Should().NotBeNull();
        found!.Email.Should().Be("carol@example.com");
        found.Name.Should().Be("Carol");
    }

    [Fact]
    public async Task GetByEmailAsync_WithUnknownEmail_ReturnsNull()
    {
        var found = await _repository.GetByEmailAsync("nobody@example.com");

        found.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangedFields()
    {
        var user = new User { Name = "Dan", Email = "dan@example.com", PasswordHash = "old-hash" };
        await _repository.CreateAsync(user);

        user.Name = "Daniel";
        user.Email = "daniel@example.com";
        user.PasswordHash = "new-hash";
        await _repository.UpdateAsync(user);

        var reloaded = await _repository.GetByIdAsync(user.UserId);
        reloaded.Should().NotBeNull();
        reloaded!.Name.Should().Be("Daniel");
        reloaded.Email.Should().Be("daniel@example.com");
        reloaded.PasswordHash.Should().Be("new-hash");
    }
}
