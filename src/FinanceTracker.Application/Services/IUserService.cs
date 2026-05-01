using FinanceTracker.Application.DTOs.Users;

namespace FinanceTracker.Application.Services;

public interface IUserService
{
    Task<UserDto> GetCurrentUserAsync(int userId, CancellationToken ct = default);
    Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request, CancellationToken ct = default);
}
