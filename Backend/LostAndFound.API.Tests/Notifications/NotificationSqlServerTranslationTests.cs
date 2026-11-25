using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Notifications;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Data;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Localization;
using LostAndFound.Api.Notifications;
using LostAndFound.Api.Services;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Notifications;

/// <summary>Runs every notification query through the SQL Server provider.</summary>
public sealed class NotificationSqlServerTranslationTests : IDisposable
{
    private readonly SqlServerDryRunInterceptor _interceptor = new();
    private readonly AppDbContext _db;

    public NotificationSqlServerTranslationTests() =>
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=dry-run;Database=Findly;TrustServerCertificate=True")
            .AddInterceptors(_interceptor)
            .Options);

    private NotificationService Service => new(_db, new FixedCurrentUser(Guid.NewGuid()), new FixedRequestLanguage());

    [Fact]
    public async Task List_with_item_and_sender_names_translates()
    {
        await Service.GetAsync(new NotificationsQuery { UnreadOnly = true, Page = 2, PageSize = 10 }, CancellationToken.None);

        string select = _interceptor.Commands.Last();
        Assert.Contains("[ItemName]", select);
        Assert.Contains("[FullName]", select);
        Assert.Contains("OFFSET", select);
    }

    [Fact]
    public async Task Unread_count_translates()
    {
        await Service.GetUnreadCountAsync(CancellationToken.None);

        Assert.Contains("[IsRead]", Assert.Single(_interceptor.Commands));
    }

    [Fact]
    public async Task Mark_one_and_mark_all_translate()
    {
        _interceptor.ExistsResult = true;

        await Service.MarkAsReadAsync(Guid.NewGuid(), CancellationToken.None);
        await Service.MarkAllAsReadAsync(CancellationToken.None);

        Assert.Equal(2, _interceptor.Commands.Count(c => c.TrimStart().StartsWith("UPDATE", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Mark_one_unknown_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service.MarkAsReadAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task New_message_settings_and_existing_notification_lookups_translate()
    {
        _interceptor.ScalarRows["[NotifyOnNewMessage]"] = true;
        var notifier = new DatabaseNotifier(_db);

        await notifier.NotifyNewMessageAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(2, _interceptor.Commands.Count);
        Assert.Contains("N'NewMessage'", _interceptor.Commands[1]);
        Assert.Single(_db.ChangeTracker.Entries<LostAndFound.Api.Data.Entities.Notification>());
    }

    [Fact]
    public async Task Resolved_recipients_and_chat_clearing_translate()
    {
        var notifier = new DatabaseNotifier(_db);

        await notifier.NotifyPostResolvedAsync(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None);
        await notifier.MarkChatNotificationsReadAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Contains("DISTINCT", _interceptor.Commands[0]);
        Assert.StartsWith("UPDATE", _interceptor.Commands[1].TrimStart());
    }

    public void Dispose() => _db.Dispose();
}
