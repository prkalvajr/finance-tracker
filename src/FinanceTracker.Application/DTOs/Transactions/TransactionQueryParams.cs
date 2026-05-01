namespace FinanceTracker.Application.DTOs.Transactions;

public record TransactionQueryParams(
    int Page = 1,
    int PageSize = 20,
    string SortBy = "date",
    string SortOrder = "desc",
    string? Category = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null);
