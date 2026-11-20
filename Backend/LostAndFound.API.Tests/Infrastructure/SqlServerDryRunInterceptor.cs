using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>
/// Lets EF Core's SQL Server provider build and "run" queries without a server:
/// the connection is never opened and every command returns a canned result.
/// The generated SQL is recorded so tests can check it.
/// </summary>
public sealed class SqlServerDryRunInterceptor : DbConnectionInterceptor, IDbCommandInterceptor
{
    public List<string> Commands { get; } = [];

    /// <summary>Answer for EXISTS checks (AnyAsync). True lets a test reach the code after an access check.</summary>
    public bool ExistsResult { get; set; }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(InterceptionResult.Suppress());

    public override Task ConnectionClosedAsync(DbConnection connection, ConnectionEndEventData eventData) => Task.CompletedTask;

    public override ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
        ValueTask.FromResult(InterceptionResult.Suppress());

    public ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Commands.Add(command.CommandText);
        return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(ResultFor(command.CommandText)));
    }

    public ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Commands.Add(command.CommandText);
        return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
    }

    /// <summary>COUNT and EXISTS queries always return one row; everything else returns no rows.</summary>
    private DbDataReader ResultFor(string sql)
    {
        string trimmed = sql.TrimStart();
        var table = new DataTable();
        if (trimmed.StartsWith("SELECT COUNT(*)", StringComparison.OrdinalIgnoreCase))
        {
            table.Columns.Add("Count", typeof(int));
            table.Rows.Add(0);
        }
        else if (trimmed.StartsWith("SELECT CASE", StringComparison.OrdinalIgnoreCase) && trimmed.Contains("EXISTS", StringComparison.Ordinal))
        {
            table.Columns.Add("Exists", typeof(bool));
            table.Rows.Add(ExistsResult);
        }

        return table.CreateDataReader();
    }
}
