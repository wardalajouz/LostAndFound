using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Notifications;
using LostAndFound.Api.Services;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LostAndFound.Api.Tests.Admin;

/// <summary>Runs every admin query through the SQL Server provider.</summary>
public sealed class AdminSqlServerTranslationTests : IDisposable
{
    private readonly SqlServerDryRunInterceptor _interceptor = new();
    private readonly AppDbContext _db;
    private readonly FixedCurrentUser _admin = new(Guid.NewGuid());

    public AdminSqlServerTranslationTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=dry-run;Database=Findly;TrustServerCertificate=True")
            .AddInterceptors(_interceptor)
            .Options);

        // The Admin role exists, so role checks are part of the generated SQL.
        _interceptor.ScalarRows["FROM [AspNetRoles]"] = Guid.NewGuid();
    }

    [Fact]
    public async Task Statistics_translate()
    {
        var service = new AdminStatsService(_db, new FixedRequestLanguage(), TimeProvider.System);

        AdminStatsResponse stats = await service.GetAsync(CancellationToken.None);

        Assert.Equal(4, _interceptor.Commands.Count);
        Assert.Contains("GROUP BY [p].[Category]", _interceptor.Commands[1]);
        Assert.Contains("[NameEn]", _interceptor.Commands[2]);
        Assert.Contains("[CreatedAt] >=", _interceptor.Commands[3]);
        Assert.Equal(12, stats.ByMonth.Count);
    }

    [Fact]
    public async Task Posts_table_with_every_filter_translates()
    {
        var service = NewPostService();

        await service.GetAsync(new AdminPostsQuery
        {
            Search = "wallet",
            Type = PostType.Lost,
            Category = ItemCategory.Bags,
            Status = PostStatus.Resolved,
            CampusLocationId = 3,
            HandedToSecurity = true,
            Visibility = PostVisibility.Hidden,
            Sort = PostSort.Oldest,
            Page = 2
        }, CancellationToken.None);

        string select = _interceptor.Commands.Last();
        Assert.Contains("[HandedToSecurity]", select);
        Assert.Contains("[Visibility]", select);
        Assert.Contains("OFFSET", select);
    }

    [Fact]
    public async Task Post_details_and_visibility_lookup_translate()
    {
        var service = NewPostService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateVisibilityAsync(
            Guid.NewGuid(), new UpdatePostVisibilityRequest { Visibility = PostVisibility.Hidden, Reason = "Spam post" }, CancellationToken.None));

        Assert.Equal(2, _interceptor.Commands.Count);
        Assert.Contains("[DeletedAt]", _interceptor.Commands[1]);
    }

    [Fact]
    public async Task Users_table_with_role_and_post_count_translates()
    {
        var service = new AdminUserService(_db, _admin, TimeProvider.System, NullLogger<AdminUserService>.Instance);

        await service.GetAsync(new AdminUsersQuery { Search = "ward", UserType = UserType.Staff, Status = AccountStatus.Disabled }, CancellationToken.None);

        string select = _interceptor.Commands.Last();
        Assert.Contains("[AspNetUserRoles]", select);
        Assert.Contains("COUNT", select);
        Assert.Contains("LIKE", select);
    }

    [Fact]
    public async Task Status_change_lookup_translates()
    {
        var service = new AdminUserService(_db, _admin, TimeProvider.System, NullLogger<AdminUserService>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateStatusAsync(
            Guid.NewGuid(), new UpdateAccountStatusRequest { Status = AccountStatus.Disabled, Reason = "Spam posts" }, CancellationToken.None));

        Assert.Contains("[AspNetUserRoles]", _interceptor.Commands.Last());
    }

    [Fact]
    public async Task Moderation_notification_lookup_translates()
    {
        await new DatabaseNotifier(_db).NotifyPostModeratedAsync(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Contains("[NotifyOnPostUpdate]", Assert.Single(_interceptor.Commands));
    }

    public void Dispose() => _db.Dispose();

    private AdminPostService NewPostService() =>
        new(_db, _admin, new FixedRequestLanguage(), new DatabaseNotifier(_db), TimeProvider.System, NullLogger<AdminPostService>.Instance);
}
