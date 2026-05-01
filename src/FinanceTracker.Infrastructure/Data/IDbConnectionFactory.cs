using System.Data.Common;

namespace FinanceTracker.Infrastructure.Data;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
