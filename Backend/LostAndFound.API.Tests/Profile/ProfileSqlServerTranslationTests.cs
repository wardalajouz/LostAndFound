using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Contracts.Support;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Email;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Images;
using LostAndFound.Api.Services;
using LostAndFound.Api.Services.Profile;
using LostAndFound.Api.Storage;
using LostAndFound.Api.Support;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Tests.Profile;

/// <summary>
/// Runs the profile and support queries through the SQL Server provider. Account deletion uses a real
/// transaction, which needs a real server: it is covered by running the suite with FINDLY_TEST_SQLSERVER.
/// </summary>
public sealed class ProfileSqlServerTranslationTests : IDisposable
{
    private readonly SqlServerDryRunInterceptor _interceptor = new();
    private readonly ServiceProvider _services;
    private readonly AsyncServiceScope _scope;

    public ProfileSqlServerTranslationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options
            .UseSqlServer("Server=dry-run;Database=Findly;TrustServerCertificate=True")
            .AddInterceptors(_interceptor));
        services.AddIdentityCore<User>().AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>();
        services.AddSingleton<ICurrentUser>(new FixedCurrentUser(Guid.NewGuid()));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddSingleton<IFileStorage, NoStorage>();
        services.AddSingleton<IEmailService, RecordingEmailService>();
        services.AddSingleton(Options.Create(new SupportOptions { InboxEmail = "support@findly.test" }));
        services.AddScoped<PasswordConfirmation>();
        services.AddScoped<AccountDeletion>();
        services.AddScoped<ProfileService>();
        services.AddScoped<SupportTicketService>();

        _services = services.BuildServiceProvider(validateScopes: true);
        _scope = _services.CreateAsyncScope();
    }

    [Fact]
    public async Task Loading_the_signed_in_user_translates()
    {
        var profile = _scope.ServiceProvider.GetRequiredService<ProfileService>();

        await Assert.ThrowsAsync<UnauthorizedException>(() => profile.GetSettingsAsync(CancellationToken.None));

        Assert.Contains("[DeletedAt] IS NULL", Assert.Single(_interceptor.Commands));
    }

    [Fact]
    public async Task Support_ticket_insert_translates()
    {
        var support = _scope.ServiceProvider.GetRequiredService<SupportTicketService>();

        await support.CreateAsync(new CreateSupportTicketRequest
        {
            FullName = "Ward",
            Email = "ward@findly.test",
            Subject = "Hello",
            Message = "A message long enough."
        }, CancellationToken.None);

        Assert.Contains("INSERT INTO [SupportTickets]", Assert.Single(_interceptor.Commands));
    }

    public void Dispose()
    {
        _scope.Dispose();
        _services.Dispose();
    }

    private sealed class NoStorage : IFileStorage
    {
        public Task<string> SaveAsync(string folder, byte[] content, string extension, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Not expected in this test.");

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
