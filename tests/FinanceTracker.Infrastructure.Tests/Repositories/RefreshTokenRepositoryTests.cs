using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Repositories;
using FluentAssertions;

namespace FinanceTracker.Infrastructure.Tests.Repositories;

[Collection("Database")]
public class RefreshTokenRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly RefreshTokenRepository _repository;
    private readonly UserRepository _userRepository;

    public RefreshTokenRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new RefreshTokenRepository(_fixture.ConnectionFactory);
        _userRepository = new UserRepository(_fixture.ConnectionFactory);
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<int> CreateUserAsync()
    {
        var user = new User { Name = "User", Email = "u@example.com", PasswordHash = "h" };
        return await _userRepository.CreateAsync(user);
    }

    [Fact]
    public async Task CreateAsync_InsertsToken()
    {
        var userId = await CreateUserAsync();
        var token = new RefreshToken
        {
            UserId = userId,
            Token = "rt-1",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
        };

        var id = await _repository.CreateAsync(token);

        id.Should().BeGreaterThan(0);
        token.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetByTokenAsync_WithExistingToken_ReturnsToken()
    {
        var userId = await CreateUserAsync();
        var expires = DateTime.UtcNow.AddDays(5);
        await _repository.CreateAsync(new RefreshToken
        {
            UserId = userId,
            Token = "rt-find",
            ExpiresAt = expires,
        });

        var found = await _repository.GetByTokenAsync("rt-find");

        found.Should().NotBeNull();
        found!.UserId.Should().Be(userId);
        found.Token.Should().Be("rt-find");
        found.ExpiresAt.Should().BeCloseTo(expires, TimeSpan.FromSeconds(2));
        found.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetByTokenAsync_WithUnknownToken_ReturnsNull()
    {
        var found = await _repository.GetByTokenAsync("does-not-exist");

        found.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAsync_SetsRevokedAt()
    {
        var userId = await CreateUserAsync();
        var token = new RefreshToken
        {
            UserId = userId,
            Token = "rt-revoke",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
        };
        await _repository.CreateAsync(token);

        await _repository.RevokeAsync(token.Id);

        var reloaded = await _repository.GetByTokenAsync("rt-revoke");
        reloaded!.RevokedAt.Should().NotBeNull();
        reloaded.RevokedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
