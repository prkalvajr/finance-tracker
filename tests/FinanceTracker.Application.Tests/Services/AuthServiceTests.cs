using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Application.Services;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace FinanceTracker.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenRepository> _tokenRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly Mock<IJwtService> _jwtMock = new();

    private AuthService CreateService() => new(
        _userRepoMock.Object,
        _tokenRepoMock.Object,
        _hasherMock.Object,
        _jwtMock.Object);

    private static AuthTokens MakeTokens(string access = "access", string refresh = "refresh") => new(
        AccessToken: access,
        RefreshToken: refresh,
        AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15),
        RefreshTokenExpiry: DateTime.UtcNow.AddDays(5));

    [Fact]
    public async Task Register_WithValidData_CreatesUserAndReturnsTokens()
    {
        var request = new RegisterRequest("Alice", "alice@example.com", "password123");
        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _hasherMock.Setup(h => h.Hash(request.Password)).Returns("hashed");
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        var tokens = MakeTokens();
        _jwtMock.Setup(j => j.GenerateTokens(42, request.Email)).Returns(tokens);

        var result = await CreateService().RegisterAsync(request);

        result.Should().Be(tokens);
        _userRepoMock.Verify(r => r.CreateAsync(
            It.Is<User>(u => u.Name == "Alice" && u.Email == "alice@example.com" && u.PasswordHash == "hashed"),
            It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepoMock.Verify(t => t.CreateAsync(
            It.Is<RefreshToken>(rt => rt.UserId == 42 && rt.Token == tokens.RefreshToken && rt.ExpiresAt == tokens.RefreshTokenExpiry),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsConflictException()
    {
        var request = new RegisterRequest("Alice", "alice@example.com", "password123");
        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = 7, Name = "Existing", Email = request.Email, PasswordHash = "x" });

        var act = () => CreateService().RegisterAsync(request);

        await act.Should().ThrowAsync<ConflictException>();
        _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_WithEmptyEmail_ThrowsValidationException(string email)
    {
        var request = new RegisterRequest("Alice", email, "password123");

        var act = () => CreateService().RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
        _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_WithEmptyName_ThrowsValidationException(string name)
    {
        var request = new RegisterRequest(name, "alice@example.com", "password123");

        var act = () => CreateService().RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
        _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_WithEmptyPassword_ThrowsValidationException(string password)
    {
        var request = new RegisterRequest("Alice", "alice@example.com", password);

        var act = () => CreateService().RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
        _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var request = new LoginRequest("alice@example.com", "password123");
        var existing = new User { UserId = 7, Name = "Alice", Email = request.Email, PasswordHash = "stored-hash" };
        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _hasherMock.Setup(h => h.Verify(request.Password, existing.PasswordHash)).Returns(true);
        var tokens = MakeTokens();
        _jwtMock.Setup(j => j.GenerateTokens(existing.UserId, existing.Email)).Returns(tokens);

        var result = await CreateService().LoginAsync(request);

        result.Should().Be(tokens);
        _tokenRepoMock.Verify(t => t.CreateAsync(
            It.Is<RefreshToken>(rt => rt.UserId == existing.UserId && rt.Token == tokens.RefreshToken && rt.ExpiresAt == tokens.RefreshTokenExpiry),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ThrowsUnauthorizedException()
    {
        var request = new LoginRequest("ghost@example.com", "password123");
        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateService().LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _tokenRepoMock.Verify(t => t.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorizedException()
    {
        var request = new LoginRequest("alice@example.com", "wrong");
        var existing = new User { UserId = 7, Name = "Alice", Email = request.Email, PasswordHash = "stored-hash" };
        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _hasherMock.Setup(h => h.Verify(request.Password, existing.PasswordHash)).Returns(false);

        var act = () => CreateService().LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _tokenRepoMock.Verify(t => t.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RevokesOldAndReturnsNewTokens()
    {
        var oldTokenString = "old-refresh";
        var stored = new RefreshToken
        {
            Id = 11,
            UserId = 7,
            Token = oldTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
            RevokedAt = null,
        };
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "x" };
        _tokenRepoMock.Setup(t => t.GetByTokenAsync(oldTokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _userRepoMock.Setup(r => r.GetByIdAsync(stored.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var newTokens = MakeTokens(access: "new-access", refresh: "new-refresh");
        _jwtMock.Setup(j => j.GenerateTokens(user.UserId, user.Email)).Returns(newTokens);

        var result = await CreateService().RefreshAsync(oldTokenString);

        result.Should().Be(newTokens);
        _tokenRepoMock.Verify(t => t.RevokeAsync(stored.Id, It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepoMock.Verify(t => t.CreateAsync(
            It.Is<RefreshToken>(rt => rt.UserId == user.UserId && rt.Token == newTokens.RefreshToken && rt.ExpiresAt == newTokens.RefreshTokenExpiry),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ThrowsUnauthorizedException()
    {
        var oldTokenString = "expired-refresh";
        var stored = new RefreshToken
        {
            Id = 11,
            UserId = 7,
            Token = oldTokenString,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RevokedAt = null,
        };
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "x" };
        _tokenRepoMock.Setup(t => t.GetByTokenAsync(oldTokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _userRepoMock.Setup(r => r.GetByIdAsync(stored.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => CreateService().RefreshAsync(oldTokenString);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _tokenRepoMock.Verify(t => t.RevokeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenRepoMock.Verify(t => t.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ThrowsUnauthorizedException()
    {
        var oldTokenString = "revoked-refresh";
        var stored = new RefreshToken
        {
            Id = 11,
            UserId = 7,
            Token = oldTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
            RevokedAt = DateTime.UtcNow.AddMinutes(-1),
        };
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "x" };
        _tokenRepoMock.Setup(t => t.GetByTokenAsync(oldTokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _userRepoMock.Setup(r => r.GetByIdAsync(stored.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => CreateService().RefreshAsync(oldTokenString);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _tokenRepoMock.Verify(t => t.RevokeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenRepoMock.Verify(t => t.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ThrowsUnauthorizedException()
    {
        _tokenRepoMock.Setup(t => t.GetByTokenAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = () => CreateService().RefreshAsync("nonexistent");

        await act.Should().ThrowAsync<UnauthorizedException>();
        _tokenRepoMock.Verify(t => t.RevokeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenRepoMock.Verify(t => t.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WithValidToken_RevokesToken()
    {
        var tokenString = "active-refresh";
        var stored = new RefreshToken
        {
            Id = 11,
            UserId = 7,
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
            RevokedAt = null,
        };
        _tokenRepoMock.Setup(t => t.GetByTokenAsync(tokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        await CreateService().LogoutAsync(tokenString);

        _tokenRepoMock.Verify(t => t.RevokeAsync(stored.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_ThrowsUnauthorizedException()
    {
        _tokenRepoMock.Setup(t => t.GetByTokenAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = () => CreateService().LogoutAsync("nonexistent");

        await act.Should().ThrowAsync<UnauthorizedException>();
        _tokenRepoMock.Verify(t => t.RevokeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
