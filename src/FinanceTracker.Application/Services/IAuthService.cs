using FinanceTracker.Application.DTOs.Auth;

namespace FinanceTracker.Application.Services;

public interface IAuthService
{
    Task<AuthTokens> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthTokens> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}
