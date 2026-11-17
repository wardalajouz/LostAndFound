using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>
/// Lets EF Core's SQL Server provider build and "run" queries without a server:
/// the connection is never opened and every command returns an empty result.
/// The generated SQL is recorded so tests can check it.
/// </summary>
public sealed class SqlServerDryRunInterceptor : DbConnectionInterceptor, IDbCommandInterceptor
{
    public List<string> Commands { get; } = [];

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
        return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(EmptyResultFor(command.CommandText)));
    }

    /// <summary>COUNT queries always return one row, so they get a single zero.</summary>
    private static DbDataReader EmptyResultFor(string sql)
    {
        var table = new DataTable();
        if (sql.TrimStart().StartsWith("SELECT COUNT(*)", StringComparison.OrdinalIgnoreCase))
        {
            table.Columns.Add("Count", typeof(int));
            table.Rows.Add(0);
        }

        return table.CreateDataReader();
    }
}
