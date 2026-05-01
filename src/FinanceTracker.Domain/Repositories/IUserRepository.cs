using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<int> CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
