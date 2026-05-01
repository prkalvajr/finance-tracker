using FinanceTracker.Application.DTOs.Transactions;

namespace FinanceTracker.Application.Services;

public interface ITransactionService
{
    Task<TransactionDto> CreateAsync(int userId, CreateTransactionRequest request, CancellationToken ct = default);
    Task<TransactionDto> UpdateAsync(int userId, UpdateTransactionRequest request, CancellationToken ct = default);
    Task<PagedResult<TransactionDto>> GetPagedAsync(int userId, TransactionQueryParams query, CancellationToken ct = default);
    Task<TransactionSummaryDto> GetSummaryAsync(int userId, CancellationToken ct = default);
    Task SoftDeleteAsync(int userId, int transactionId, CancellationToken ct = default);
}
