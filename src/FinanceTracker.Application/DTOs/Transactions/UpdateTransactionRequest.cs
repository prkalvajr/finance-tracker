namespace FinanceTracker.Application.DTOs.Transactions;

public record UpdateTransactionRequest(
    int TransactionId,
    string Title,
    decimal Amount,
    string Category,
    DateOnly? Date);
