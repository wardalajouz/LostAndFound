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

/// <summary>Runs the real API in memory, with an in-memory SQLite database and a recording email service.</summary>
public class FindlyApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FindlyApiFactory()
    {
        _connection.Open();
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        context.Database.EnsureCreated();
    }

    public RecordingEmailService Emails { get; } = new();

    protected virtual IReadOnlyDictionary<string, string> Settings => new Dictionary<string, string>
    {
        ["ConnectionStrings:Default"] = "Server=replaced-in-tests",
        ["Jwt:SigningKey"] = SigningKey
    };

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
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

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
        }
    }
}
