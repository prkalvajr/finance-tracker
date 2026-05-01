namespace FinanceTracker.Application.DTOs.Transactions;

public record TransactionSummaryDto(
    decimal NetBalance,
    decimal TotalIncome,
    decimal TotalExpense);
