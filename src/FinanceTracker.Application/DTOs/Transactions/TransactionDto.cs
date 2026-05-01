namespace FinanceTracker.Application.DTOs.Transactions;

public record TransactionDto(
    int TransactionId,
    int UserId,
    string Title,
    decimal Amount,
    string Category,
    DateOnly Date,
    bool Deleted,
    DateTime CreatedAt,
    DateTime UpdatedAt);
