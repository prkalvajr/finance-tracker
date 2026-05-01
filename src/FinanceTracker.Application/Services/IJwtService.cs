using FinanceTracker.Application.DTOs.Auth;

namespace FinanceTracker.Application.Services;

public interface IJwtService
{
    AuthTokens GenerateTokens(int userId, string email);
}
