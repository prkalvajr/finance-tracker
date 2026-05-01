using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Domain.Repositories;

namespace FinanceTracker.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");
        return new UserDto(user.UserId, user.Name, user.Email);
    }

    public async Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            user.Name = request.Name;

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            var existing = await _userRepository.GetByEmailAsync(request.Email, ct);
            if (existing is not null && existing.UserId != userId)
                throw new ConflictException("Email is already in use.");
            user.Email = request.Email;
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                throw new ValidationException("Current password is required to change password.");
            if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedException("Current password is incorrect.");
            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        }

        await _userRepository.UpdateAsync(user, ct);
        return new UserDto(user.UserId, user.Name, user.Email);
    }
}
