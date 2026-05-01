namespace FinanceTracker.Application.DTOs.Users;

public record UpdateUserRequest(
    string? Name,
    string? Email,
    string? CurrentPassword,
    string? NewPassword);
