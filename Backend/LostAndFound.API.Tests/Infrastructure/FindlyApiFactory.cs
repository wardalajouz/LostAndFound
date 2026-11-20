using LostAndFound.Api.Data;
using LostAndFound.Api.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>
/// Runs the real API in memory, with an in-memory SQLite database, a recording email service,
/// and a temporary folder for uploaded files.
/// </summary>
public class FindlyApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FindlyApiFactory()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>();
        UseTestDatabase(options);
        using var context = new AppDbContext(options.Options);
        context.Database.EnsureCreated();
    }

    public RecordingEmailService Emails { get; } = new();

    public SaveChangesHooks SaveHooks { get; } = new();

    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), $"findly-tests-{Guid.NewGuid():N}");

    protected virtual IReadOnlyDictionary<string, string> Settings => new Dictionary<string, string>
    {
        ["ConnectionStrings:Default"] = "Server=replaced-in-tests",
        ["Jwt:SigningKey"] = SigningKey,
        ["Storage:RootPath"] = StorageRoot
    };

    /// <summary>Runs code against the test database, for setting up or checking data directly.</summary>
    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Every stored file, as a path relative to the storage folder with forward slashes.</summary>
    public IReadOnlyList<string> StoredFiles() =>
        Directory.Exists(StorageRoot)
            ? Directory.GetFiles(StorageRoot, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(StorageRoot, path).Replace('\\', '/'))
                .ToList()
            : [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        foreach ((string key, string value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => UseTestDatabase(options).AddInterceptors(SaveHooks));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            if (Directory.Exists(StorageRoot))
            {
                Directory.Delete(StorageRoot, recursive: true);
            }
        }
    }

    private DbContextOptionsBuilder UseTestDatabase(DbContextOptionsBuilder options) =>
        options.UseSqlite(_connection).ReplaceService<IModelCustomizer, SqliteTestModelCustomizer>();
}
