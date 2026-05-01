using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;

namespace FinanceTracker.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthTokens> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Password is required.");

        if (await _userRepository.GetByEmailAsync(request.Email, ct) is not null)
            throw new ConflictException("Email is already in use.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
        };
        user.UserId = await _userRepository.CreateAsync(user, ct);

        var tokens = _jwtService.GenerateTokens(user.UserId, user.Email);

        await _refreshTokenRepository.CreateAsync(new RefreshToken
        {
            UserId = user.UserId,
            Token = tokens.RefreshToken,
            ExpiresAt = tokens.RefreshTokenExpiry,
        }, ct);

        return tokens;
    }

    public async Task<AuthTokens> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials.");

        var tokens = _jwtService.GenerateTokens(user.UserId, user.Email);

        await _refreshTokenRepository.CreateAsync(new RefreshToken
        {
            UserId = user.UserId,
            Token = tokens.RefreshToken,
            ExpiresAt = tokens.RefreshTokenExpiry,
        }, ct);

        return tokens;
    }

    public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await _refreshTokenRepository.GetByTokenAsync(refreshToken, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (stored.RevokedAt is not null)
            throw new UnauthorizedException("Refresh token has been revoked.");

        if (stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired.");

        var user = await _userRepository.GetByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        await _refreshTokenRepository.RevokeAsync(stored.Id, ct);

        var tokens = _jwtService.GenerateTokens(user.UserId, user.Email);

        await _refreshTokenRepository.CreateAsync(new RefreshToken
        {
            UserId = user.UserId,
            Token = tokens.RefreshToken,
            ExpiresAt = tokens.RefreshTokenExpiry,
        }, ct);

        return tokens;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await _refreshTokenRepository.GetByTokenAsync(refreshToken, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        await _refreshTokenRepository.RevokeAsync(stored.Id, ct);
    }
}
