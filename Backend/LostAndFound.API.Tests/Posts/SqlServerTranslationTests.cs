using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Data;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Images;
using LostAndFound.Api.Localization;
using LostAndFound.Api.Notifications;
using LostAndFound.Api.Services;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Posts;

/// <summary>
/// The other tests run on SQLite. These run the same service code through the SQL Server provider,
/// so a query that SQL Server cannot translate fails here instead of in production.
/// </summary>
public sealed class SqlServerTranslationTests : IDisposable
{
    private readonly SqlServerDryRunInterceptor _interceptor = new();
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user = new FixedCurrentUser(Guid.NewGuid());
    private readonly IRequestLanguage _language = new FixedRequestLanguage(PreferredLanguage.English);

    public SqlServerTranslationTests()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=dry-run;Database=Findly;TrustServerCertificate=True")
            .AddInterceptors(_interceptor)
            .Options;
        _db = new AppDbContext(options);
    }

    [Fact]
    public async Task Browse_with_every_filter_translates_to_sql_server()
    {
        var service = new PostService(_db, _user, _language, new DatabaseNotifier(_db), TimeProvider.System);
        var query = new BrowsePostsQuery
        {
            Search = "100%_[x]",
            Type = PostType.Found,
            Category = ItemCategory.Electronics,
            CampusLocationId = 3,
            Date = new DateOnly(2026, 11, 1),
            Sort = PostSort.Oldest,
            Page = 2,
            PageSize = 10
        };

        await service.BrowseAsync(query, CancellationToken.None);

        string select = _interceptor.Commands.Last();
        Assert.Contains("LIKE", select);
        Assert.Contains("ESCAPE N'\\'", select);
        Assert.Contains("OFFSET", select);
        Assert.Contains("ORDER BY [p].[CreatedAt], [p].[Id]", select);
    }

    [Fact]
    public async Task Post_details_translate_to_sql_server()
    {
        var service = new PostService(_db, _user, _language, new DatabaseNotifier(_db), TimeProvider.System);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Single(_interceptor.Commands);
    }

    [Fact]
    public async Task My_posts_list_details_and_stats_translate_to_sql_server()
    {
        var service = new MyPostsService(_db, _user, _language);

        await service.GetAsync(new MyPostsQuery { Search = "umbrella", Type = PostType.Lost, Status = PostStatus.Active }, CancellationToken.None);
        PostStatsResponse stats = await service.GetStatsAsync(CancellationToken.None);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(new PostStatsResponse(0, 0, 0, 0), stats);
        Assert.Equal(4, _interceptor.Commands.Count);
    }

    [Fact]
    public async Task Campus_locations_translate_to_sql_server()
    {
        var service = new CampusLocationService(_db, _language);

        await service.GetAllAsync(CancellationToken.None);

        Assert.Contains("[NameEn]", Assert.Single(_interceptor.Commands));
    }

    [Fact]
    public async Task Post_image_ownership_lookup_translates_to_sql_server()
    {
        var service = new PostImageService(_db, _user, new SkiaImageProcessor(), new NoStorage(), TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PostImageService>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("[OwnerId]", Assert.Single(_interceptor.Commands));
    }

    private sealed class NoStorage : LostAndFound.Api.Storage.IFileStorage
    {
        public Task<string> SaveAsync(string folder, byte[] content, string extension, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Not expected in this test.");

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public void Dispose() => _db.Dispose();
}
