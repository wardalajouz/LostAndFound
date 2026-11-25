using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Localization;
using LostAndFound.Api.Services;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Admin;

/// <summary>Statistics count the whole database, so each test gets its own.</summary>
public sealed class AdminStatsTests : IAsyncLifetime
{
    private readonly AdminApiFactory _factory = new();
    private HttpClient _client = null!;
    private string _admin = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _admin = await _client.AdminTokenAsync();
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Empty_site_has_zeros_every_category_and_twelve_months()
    {
        AdminStatsResponse stats = await _client.AdminStatsAsync(_admin);

        Assert.Equal((0, 0, 0, 0), (stats.LostCount, stats.FoundCount, stats.ActiveCount, stats.ResolvedCount));
        Assert.Equal(Enum.GetValues<ItemCategory>(), stats.ByCategory.Select(c => c.Category));
        Assert.All(stats.ByCategory, c => Assert.Equal((0, 0), (c.LostCount, c.FoundCount)));
        Assert.Empty(stats.ByCampusLocation);
        Assert.Equal(AdminStatsService.MonthsShown, stats.ByMonth.Count);
        Assert.All(stats.ByMonth, m => Assert.Equal((0, 0), (m.LostCount, m.FoundCount)));
    }

    [Fact]
    public async Task Months_are_consecutive_and_end_with_the_current_campus_month()
    {
        AdminStatsResponse stats = await _client.AdminStatsAsync(_admin);

        DateTimeOffset localNow = CampusTime.ToLocal(DateTimeOffset.UtcNow);
        var expected = Enumerable.Range(0, 12)
            .Select(i => new DateOnly(localNow.Year, localNow.Month, 1).AddMonths(i - 11))
            .Select(d => (d.Year, d.Month));
        Assert.Equal(expected, stats.ByMonth.Select(m => (m.Year, m.Month)));
    }

    [Fact]
    public async Task Totals_and_breakdowns_count_hidden_posts_but_not_deleted_ones()
    {
        string owner = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        await _client.CreatePostAsync(owner, PostApi.Body(type: "Lost", category: "Electronics", campusLocationId: PostApi.LibraryLocationId));
        await _client.CreatePostAsync(owner, PostApi.Body(type: "Lost", category: "Electronics", campusLocationId: PostApi.LibraryLocationId));
        await _client.CreatePostAsync(owner, PostApi.Body(type: "Found", category: "Keys", campusLocationId: PostApi.MainGateLocationId));
        PostResponse resolved = await _client.CreatePostAsync(owner, PostApi.Body(type: "Found", category: "Keys", campusLocationId: PostApi.LibraryLocationId));
        PostResponse hidden = await _client.CreatePostAsync(owner, PostApi.Body(type: "Lost", category: "Books", campusLocationId: PostApi.MainGateLocationId));
        PostResponse deleted = await _client.CreatePostAsync(owner, PostApi.Body(type: "Found", category: "Bags", campusLocationId: PostApi.MainGateLocationId));
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{resolved.Id}/resolve", owner);
        await _client.SetVisibilityAsync(_admin, hidden.Id, "Hidden");
        await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{deleted.Id}", owner);

        AdminStatsResponse stats = await _client.AdminStatsAsync(_admin, language: "en");

        Assert.Equal((3, 2, 4, 1), (stats.LostCount, stats.FoundCount, stats.ActiveCount, stats.ResolvedCount));
        Assert.Equal((2, 0), Counts(stats, ItemCategory.Electronics));
        Assert.Equal((0, 2), Counts(stats, ItemCategory.Keys));
        Assert.Equal((1, 0), Counts(stats, ItemCategory.Books));
        Assert.Equal((0, 0), Counts(stats, ItemCategory.Bags));

        Assert.Equal(
            [("Library", 2, 1), ("Main Gate", 1, 1)],
            stats.ByCampusLocation.Select(l => (l.CampusLocation.Name, l.LostCount, l.FoundCount)));

        MonthlyStatsResponse thisMonth = stats.ByMonth[^1];
        Assert.Equal((3, 2), (thisMonth.LostCount, thisMonth.FoundCount));
    }

    [Fact]
    public async Task Location_names_follow_the_requested_language()
    {
        string owner = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        await _client.CreatePostAsync(owner, PostApi.Body(campusLocationId: PostApi.LibraryLocationId));

        AdminStatsResponse stats = await _client.AdminStatsAsync(_admin);

        Assert.Equal("المكتبة", Assert.Single(stats.ByCampusLocation).CampusLocation.Name);
    }

    [Fact]
    public async Task Months_follow_campus_time_and_stop_after_twelve()
    {
        DateTimeOffset localNow = CampusTime.ToLocal(DateTimeOffset.UtcNow);
        DateTimeOffset thisMonthStart = CampusTime.StartOfMonth(localNow.Year, localNow.Month);
        var firstShown = new DateOnly(localNow.Year, localNow.Month, 1).AddMonths(-11);
        DateTimeOffset windowStart = CampusTime.StartOfMonth(firstShown.Year, firstShown.Month);

        // A minute after midnight on the 1st in Jordan is still the previous month in UTC.
        await PostCreatedAtAsync(thisMonthStart.AddMinutes(1), "Lost");
        await PostCreatedAtAsync(thisMonthStart.AddMinutes(-1), "Found");
        await PostCreatedAtAsync(windowStart, "Lost");
        await PostCreatedAtAsync(windowStart.AddMinutes(-1), "Lost");

        AdminStatsResponse stats = await _client.AdminStatsAsync(_admin);

        Assert.Equal((1, 0), (stats.ByMonth[^1].LostCount, stats.ByMonth[^1].FoundCount));
        Assert.Equal((0, 1), (stats.ByMonth[^2].LostCount, stats.ByMonth[^2].FoundCount));
        Assert.Equal((1, 0), (stats.ByMonth[0].LostCount, stats.ByMonth[0].FoundCount));
        Assert.Equal(4, stats.LostCount + stats.FoundCount);
        Assert.Equal(3, stats.ByMonth.Sum(m => m.LostCount + m.FoundCount));
    }

    private static (int Lost, int Found) Counts(AdminStatsResponse stats, ItemCategory category)
    {
        CategoryStatsResponse row = stats.ByCategory.Single(c => c.Category == category);
        return (row.LostCount, row.FoundCount);
    }

    private async Task PostCreatedAtAsync(DateTimeOffset createdAt, string type)
    {
        string owner = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        PostResponse post = await _client.CreatePostAsync(owner, PostApi.Body(type: type));
        await _factory.WithDbAsync(db => db.Posts.Where(p => p.Id == post.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CreatedAt, createdAt.ToUniversalTime())));
    }
}
