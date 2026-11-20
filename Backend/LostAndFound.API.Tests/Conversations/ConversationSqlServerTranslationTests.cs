using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Data;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Services;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Conversations;

/// <summary>Runs every chat query through the SQL Server provider, so untranslatable LINQ fails here, not in production.</summary>
public sealed class ConversationSqlServerTranslationTests : IDisposable
{
    private readonly SqlServerDryRunInterceptor _interceptor = new();
    private readonly AppDbContext _db;
    private readonly ConversationService _service;

    public ConversationSqlServerTranslationTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=dry-run;Database=Findly;TrustServerCertificate=True")
            .AddInterceptors(_interceptor)
            .Options);
        _service = new ConversationService(_db, new FixedUser(Guid.NewGuid()), TimeProvider.System);
    }

    [Fact]
    public async Task Inbox_query_with_last_message_unread_count_and_ordering_translates()
    {
        await _service.GetAllAsync(new PagedQuery { Page = 2, PageSize = 10 }, CancellationToken.None);

        string select = _interceptor.Commands.Last();
        Assert.Contains("[LastMessageAt]", select);
        Assert.Contains("COALESCE", select);
        Assert.Contains("OFFSET", select);
    }

    [Fact]
    public async Task Single_chat_query_translates()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("[StarterId]", Assert.Single(_interceptor.Commands));
    }

    [Fact]
    public async Task Start_post_lookup_translates()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.StartAsync(new StartConversationRequest { PostId = Guid.NewGuid() }, CancellationToken.None));

        Assert.Contains("[Visibility]", Assert.Single(_interceptor.Commands));
    }

    [Fact]
    public async Task Message_list_translates()
    {
        _interceptor.ExistsResult = true;

        await _service.GetMessagesAsync(Guid.NewGuid(), new PagedQuery(), CancellationToken.None);

        Assert.Equal(3, _interceptor.Commands.Count);
        Assert.Contains("ORDER BY [m].[SentAt] DESC", _interceptor.Commands.Last());
    }

    [Fact]
    public async Task Send_participant_lookup_translates()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.SendMessageAsync(Guid.NewGuid(), new SendMessageRequest { Content = "hi" }, CancellationToken.None));

        Assert.Single(_interceptor.Commands);
    }

    [Fact]
    public async Task Mark_as_read_bulk_update_translates()
    {
        _interceptor.ExistsResult = true;

        await _service.MarkAsReadAsync(Guid.NewGuid(), CancellationToken.None);

        string update = _interceptor.Commands.Last();
        Assert.StartsWith("UPDATE", update.TrimStart());
        Assert.Contains("[ReadAt] IS NULL", update);
    }

    public void Dispose() => _db.Dispose();

    private sealed class FixedUser(Guid id) : ICurrentUser
    {
        public Guid Id { get; } = id;
    }
}
