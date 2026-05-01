using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(int transactionId, int userId, CancellationToken ct = default);
    Task<int> CreateAsync(Transaction transaction, CancellationToken ct = default);
    Task UpdateAsync(Transaction transaction, CancellationToken ct = default);
    Task SoftDeleteAsync(int transactionId, int userId, CancellationToken ct = default);

    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetPagedAsync(
        int userId,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        string? category,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct = default);

    Task<(decimal NetBalance, decimal TotalIncome, decimal TotalExpense)> GetSummaryAsync(
        int userId,
        CancellationToken ct = default);
}
