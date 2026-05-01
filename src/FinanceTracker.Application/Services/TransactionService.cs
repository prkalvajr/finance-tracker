using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;

namespace FinanceTracker.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;

    public TransactionService(ITransactionRepository repository)
    {
        _repository = repository;
    }

    private static readonly string[] ValidCategories = { "income", "expense" };

    private static void Validate(string title, decimal amount, string category)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("Title is required.");

        if (amount <= 0)
            throw new ValidationException("Amount must be greater than zero.");

        if (Array.IndexOf(ValidCategories, category) < 0)
            throw new ValidationException("Category must be 'income' or 'expense'.");
    }

    public async Task<TransactionDto> CreateAsync(int userId, CreateTransactionRequest request, CancellationToken ct = default)
    {
        Validate(request.Title, request.Amount, request.Category);

        var now = DateTime.UtcNow;
        var transaction = new Transaction
        {
            UserId = userId,
            Title = request.Title,
            Amount = request.Amount,
            Category = request.Category,
            Date = request.Date ?? DateOnly.FromDateTime(now),
            Deleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        transaction.TransactionId = await _repository.CreateAsync(transaction, ct);
        return ToDto(transaction);
    }

    private static TransactionDto ToDto(Transaction t) => new(
        t.TransactionId, t.UserId, t.Title, t.Amount, t.Category, t.Date, t.Deleted, t.CreatedAt, t.UpdatedAt);

    public async Task<TransactionDto> UpdateAsync(int userId, UpdateTransactionRequest request, CancellationToken ct = default)
    {
        Validate(request.Title, request.Amount, request.Category);

        var existing = await _repository.GetByIdAsync(request.TransactionId, userId, ct)
            ?? throw new NotFoundException("Transaction not found.");

        existing.Title = request.Title;
        existing.Amount = request.Amount;
        existing.Category = request.Category;
        existing.Date = request.Date ?? existing.Date;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing, ct);
        return ToDto(existing);
    }

    public async Task<PagedResult<TransactionDto>> GetPagedAsync(int userId, TransactionQueryParams query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            userId,
            query.Page,
            query.PageSize,
            query.SortBy,
            query.SortOrder,
            query.Category,
            query.DateFrom,
            query.DateTo,
            ct);

        var dtos = items.Select(ToDto).ToList();
        return new PagedResult<TransactionDto>(dtos, totalCount, query.Page, query.PageSize);
    }

    public async Task<TransactionSummaryDto> GetSummaryAsync(int userId, CancellationToken ct = default)
    {
        var (net, income, expense) = await _repository.GetSummaryAsync(userId, ct);
        return new TransactionSummaryDto(net, income, expense);
    }

    public async Task SoftDeleteAsync(int userId, int transactionId, CancellationToken ct = default)
    {
        if (await _repository.GetByIdAsync(transactionId, userId, ct) is null)
            throw new NotFoundException("Transaction not found.");

        await _repository.SoftDeleteAsync(transactionId, userId, ct);
    }
}
