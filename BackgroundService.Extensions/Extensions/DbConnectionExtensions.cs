using Dapper;
using System.Data;

namespace BackgroundServiceExtensions.Extensions;

public static class DbConnectionExtensions
{
    public static async Task<(TMessage Message, bool Success)> DequeueAsync<TMessage>(this IDbConnection connection, string tableName, string? criteria = null, object? parameters = null)
    {
        string sql = $"DELETE TOP (1) FROM {tableName} WITH (ROWLOCK, READPAST) OUTPUT [deleted].*";
        if (!string.IsNullOrEmpty(criteria)) sql += $" WHERE {criteria}";

        TMessage message = await connection.QuerySingleOrDefaultAsync<TMessage>(sql, parameters);

        return (message, (message is not null));
    }
}
