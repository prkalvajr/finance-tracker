using System.Data.Common;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Infrastructure.Data;

namespace FinanceTracker.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private static readonly HashSet<string> AllowedSortColumns =
        new(StringComparer.OrdinalIgnoreCase) { "date", "amount", "title", "category", "created_at" };

    private readonly IDbConnectionFactory _connectionFactory;

    public TransactionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(Transaction transaction, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO transactions (user_id, title, amount, category, date, deleted, created_at, updated_at)
            VALUES (@user_id, @title, @amount, @category, @date, FALSE, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
            RETURNING transaction_id, created_at, updated_at;
            """;
        AddParameter(cmd, "user_id", transaction.UserId);
        AddParameter(cmd, "title", transaction.Title);
        AddParameter(cmd, "amount", transaction.Amount);
        AddParameter(cmd, "category", transaction.Category);
        AddParameter(cmd, "date", transaction.Date);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        transaction.TransactionId = reader.GetInt32(0);
        transaction.CreatedAt = reader.GetDateTime(1);
        transaction.UpdatedAt = reader.GetDateTime(2);
        return transaction.TransactionId;
    }

    public async Task<Transaction?> GetByIdAsync(int transactionId, int userId, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT transaction_id, user_id, title, amount, category, date, deleted, created_at, updated_at
              FROM transactions
             WHERE transaction_id = @id
               AND user_id = @user_id
               AND deleted = FALSE;
            """;
        AddParameter(cmd, "id", transactionId);
        AddParameter(cmd, "user_id", userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE transactions
               SET title = @title,
                   amount = @amount,
                   category = @category,
                   date = @date,
                   updated_at = NOW() AT TIME ZONE 'UTC'
             WHERE transaction_id = @id
               AND user_id = @user_id
               AND deleted = FALSE
            RETURNING updated_at;
            """;
        AddParameter(cmd, "id", transaction.TransactionId);
        AddParameter(cmd, "user_id", transaction.UserId);
        AddParameter(cmd, "title", transaction.Title);
        AddParameter(cmd, "amount", transaction.Amount);
        AddParameter(cmd, "category", transaction.Category);
        AddParameter(cmd, "date", transaction.Date);

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is DateTime updatedAt)
        {
            transaction.UpdatedAt = updatedAt;
        }
    }

    public async Task SoftDeleteAsync(int transactionId, int userId, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE transactions
               SET deleted = TRUE,
                   updated_at = NOW() AT TIME ZONE 'UTC'
             WHERE transaction_id = @id
               AND user_id = @user_id;
            """;
        AddParameter(cmd, "id", transactionId);
        AddParameter(cmd, "user_id", userId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetPagedAsync(
        int userId,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        string? category,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var sortColumn = AllowedSortColumns.Contains(sortBy) ? sortBy.ToLowerInvariant() : "date";
        var sortDirection = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        var whereClauses = new List<string> { "user_id = @user_id", "deleted = FALSE" };
        if (!string.IsNullOrWhiteSpace(category))
            whereClauses.Add("category = @category");
        if (dateFrom.HasValue)
            whereClauses.Add("date >= @date_from");
        if (dateTo.HasValue)
            whereClauses.Add("date <= @date_to");

        var where = "WHERE " + string.Join(" AND ", whereClauses);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        int totalCount;
        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = $"SELECT COUNT(*) FROM transactions {where};";
            AddPagedFilterParameters(countCmd, userId, category, dateFrom, dateTo);
            totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
        }

        var items = new List<Transaction>();
        await using (var pageCmd = connection.CreateCommand())
        {
            pageCmd.CommandText = $"""
                SELECT transaction_id, user_id, title, amount, category, date, deleted, created_at, updated_at
                  FROM transactions
                  {where}
                 ORDER BY {sortColumn} {sortDirection}, transaction_id {sortDirection}
                 LIMIT @limit OFFSET @offset;
                """;
            AddPagedFilterParameters(pageCmd, userId, category, dateFrom, dateTo);
            AddParameter(pageCmd, "limit", pageSize);
            AddParameter(pageCmd, "offset", (page - 1) * pageSize);

            await using var reader = await pageCmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(Map(reader));
            }
        }

        return (items, totalCount);
    }

    public async Task<(decimal NetBalance, decimal TotalIncome, decimal TotalExpense)> GetSummaryAsync(
        int userId,
        CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                COALESCE(SUM(CASE WHEN category = 'income'  THEN amount ELSE 0 END), 0) AS total_income,
                COALESCE(SUM(CASE WHEN category = 'expense' THEN amount ELSE 0 END), 0) AS total_expense
              FROM transactions
             WHERE user_id = @user_id
               AND deleted = FALSE;
            """;
        AddParameter(cmd, "user_id", userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);

        var totalIncome = reader.GetDecimal(0);
        var totalExpense = reader.GetDecimal(1);
        return (totalIncome - totalExpense, totalIncome, totalExpense);
    }

    private static void AddPagedFilterParameters(
        DbCommand cmd,
        int userId,
        string? category,
        DateOnly? dateFrom,
        DateOnly? dateTo)
    {
        AddParameter(cmd, "user_id", userId);
        if (!string.IsNullOrWhiteSpace(category))
            AddParameter(cmd, "category", category);
        if (dateFrom.HasValue)
            AddParameter(cmd, "date_from", dateFrom.Value);
        if (dateTo.HasValue)
            AddParameter(cmd, "date_to", dateTo.Value);
    }

    private static Transaction Map(DbDataReader reader) => new()
    {
        TransactionId = reader.GetInt32(0),
        UserId = reader.GetInt32(1),
        Title = reader.GetString(2),
        Amount = reader.GetDecimal(3),
        Category = reader.GetString(4),
        Date = DateOnly.FromDateTime(reader.GetDateTime(5)),
        Deleted = reader.GetBoolean(6),
        CreatedAt = reader.GetDateTime(7),
        UpdatedAt = reader.GetDateTime(8),
    };

    private static void AddParameter(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
