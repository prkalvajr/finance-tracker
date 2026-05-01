using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<int> CreateAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task RevokeAsync(int id, CancellationToken ct = default);
}
