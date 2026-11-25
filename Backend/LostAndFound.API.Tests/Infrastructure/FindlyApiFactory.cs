using LostAndFound.Api.Data;
using LostAndFound.Api.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>
/// Runs the real API in memory, with its own test database (see <see cref="TestDatabase"/>),
/// a recording email service, and a temporary folder for uploaded files.
/// </summary>
public class FindlyApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256";

    private readonly TestDatabase _database = TestDatabase.Create();

    public RecordingEmailService Emails { get; } = new();

    public SaveChangesHooks SaveHooks { get; } = new();

    public TestLogSink Logs { get; } = new();

    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), $"findly-tests-{Guid.NewGuid():N}");

    /// <summary>The ASP.NET Core environment the API runs in. Production turns off Swagger and developer error details.</summary>
    protected virtual string EnvironmentName => "Development";

    protected virtual IReadOnlyDictionary<string, string> Settings => new Dictionary<string, string>
    {
        ["ConnectionStrings:Default"] = "Server=replaced-in-tests",
        ["Jwt:SigningKey"] = SigningKey,
        ["Storage:RootPath"] = StorageRoot,

        // Tests make many requests from one in-memory client; the limits themselves are tested in RateLimitTests.
        ["RateLimiting:Auth:PermitLimit"] = "100000",
        ["RateLimiting:Support:PermitLimit"] = "100000",
        ["RateLimiting:Messages:PermitLimit"] = "100000"
    };

    /// <summary>Runs code against the test database, for setting up or checking data directly.</summary>
    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Creates an account directly, without the register endpoint, for tests that must not use up the auth rate limit.</summary>
    public async Task CreateUserAsync(string email, string fullName = ApiClientExtensions.DefaultFullName)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<LostAndFound.Api.Data.Entities.User>>();
        var user = new LostAndFound.Api.Data.Entities.User
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        Microsoft.AspNetCore.Identity.IdentityResult result = await users.CreateAsync(user, ApiClientExtensions.ValidPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>A valid access token for an existing account, made directly rather than through the login endpoint.</summary>
    public async Task<string> TokenForAsync(string email)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<LostAndFound.Api.Data.Entities.User>>();
        var user = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException($"No account for {email}.");
        var tokens = scope.ServiceProvider.GetRequiredService<LostAndFound.Api.Auth.ITokenService>();
        return tokens.CreateAccessToken(user, await users.GetRolesAsync(user)).AccessToken;
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
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        foreach ((string key, string value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => _database.Configure(options).AddInterceptors(SaveHooks));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _database.Dispose();
            if (Directory.Exists(StorageRoot))
            {
                Directory.Delete(StorageRoot, recursive: true);
            }
        }
    }
}
