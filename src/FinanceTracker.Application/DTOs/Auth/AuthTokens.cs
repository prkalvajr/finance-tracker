namespace FinanceTracker.Application.DTOs.Auth;

public record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry);
