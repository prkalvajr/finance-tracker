namespace FinanceTracker.Application.DTOs.Transactions;

public record CreateTransactionRequest(
    string Title,
    decimal Amount,
    string Category,
    DateOnly? Date);
