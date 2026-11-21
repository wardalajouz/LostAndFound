using LostAndFound.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>
/// The database a test API instance runs on. By default an in-memory SQLite database.
/// Set the environment variable <see cref="SqlServerVariable"/> to a SQL Server connection string
/// (without a database name) to run every test on real SQL Server instead: each test API instance
/// then gets its own new database, created from the real migrations and dropped afterwards.
/// </summary>
public abstract class TestDatabase : IDisposable
{
    public const string SqlServerVariable = "FINDLY_TEST_SQLSERVER";

    public static TestDatabase Create() =>
        Environment.GetEnvironmentVariable(SqlServerVariable) is { Length: > 0 } server
            ? new SqlServerTestDatabase(server)
            : new SqliteTestDatabase();

    public abstract DbContextOptionsBuilder Configure(DbContextOptionsBuilder options);

    public abstract void Dispose();

    protected AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        Configure(options);
        return new AppDbContext(options.Options);
    }
}

public sealed class SqliteTestDatabase : TestDatabase
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using AppDbContext context = NewContext();
        context.Database.EnsureCreated();
    }

    public override DbContextOptionsBuilder Configure(DbContextOptionsBuilder options) =>
        options.UseSqlite(_connection).ReplaceService<IModelCustomizer, SqliteTestModelCustomizer>();

    public override void Dispose() => _connection.Dispose();
}

public sealed class SqlServerTestDatabase : TestDatabase
{
    private readonly string _connectionString;

    public SqlServerTestDatabase(string server)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(server)
        {
            InitialCatalog = $"findly_test_{Guid.NewGuid():N}"
        };
        _connectionString = builder.ConnectionString;

        using AppDbContext context = NewContext();
        context.Database.Migrate();
    }

    public override DbContextOptionsBuilder Configure(DbContextOptionsBuilder options) =>
        options.UseSqlServer(_connectionString);

    public override void Dispose()
    {
        using AppDbContext context = NewContext();
        context.Database.EnsureDeleted();
    }
}
