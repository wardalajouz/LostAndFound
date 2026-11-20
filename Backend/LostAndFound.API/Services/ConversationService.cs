using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Services.Conversations;
using LostAndFound.Api.Services.Posts;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

/// <summary>
/// Chats between a user and a post's owner. Every operation is limited to the two participants;
/// anyone else gets the same 404 as for a chat that does not exist.
/// </summary>
public sealed class ConversationService(AppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    : IConversationService
{
    private const string ConversationNotFound = "Conversation not found.";

    public async Task<ConversationResponse> StartAsync(StartConversationRequest request, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.Id;

        var post = await db.Posts
            .AsNoTracking()
            .Where(p => p.Id == request.PostId && p.Visibility == PostVisibility.Visible)
            .Select(p => new { p.Id, p.OwnerId, p.Status })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Post not found.");

        if (post.OwnerId == userId)
        {
            throw new RequestValidationException(nameof(StartConversationRequest.PostId), "You cannot message yourself about your own post.");
        }

        // An existing chat stays reachable even after the post is resolved.
        Guid? existingId = await FindStartedByAsync(post.Id, userId, cancellationToken);
        if (existingId is { } id)
        {
            return await LoadAsync(id, userId, cancellationToken);
        }

        if (post.Status != PostStatus.Active)
        {
            throw new ConflictException("This post is resolved, so new conversations cannot be started.");
        }

        var conversation = new Conversation { PostId = post.Id, StarterId = userId, CreatedAt = timeProvider.GetUtcNow() };
        db.Conversations.Add(conversation);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another request created the same chat a moment ago (the unique index on PostId + StarterId).
            // Use that one; if there is none, the save failed for another reason, so rethrow the original error.
            db.ChangeTracker.Clear();
            Guid? winnerId = await FindStartedByAsync(post.Id, userId, cancellationToken);
            if (winnerId is null)
            {
                throw;
            }

            return await LoadAsync(winnerId.Value, userId, cancellationToken);
        }

        return await LoadAsync(conversation.Id, userId, cancellationToken);
    }

    public async Task<PagedResponse<ConversationResponse>> GetAllAsync(PagedQuery query, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.Id;
        IQueryable<Conversation> inbox = db.Conversations.AsNoTracking().VisibleInInboxOf(userId);

        int totalCount = await inbox.CountAsync(cancellationToken);
        return await inbox
            .ByLatestActivity()
            .Select(ConversationQueries.ToResponse(userId))
            .ToPagedResponseAsync(totalCount, query, cancellationToken);
    }

    public async Task<ConversationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await LoadAsync(id, currentUser.Id, cancellationToken);

    /// <summary>Newest first. Reading does not mark messages as read; the client calls MarkAsRead for that.</summary>
    public async Task<PagedResponse<MessageResponse>> GetMessagesAsync(
        Guid conversationId, PagedQuery query, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.Id;
        await EnsureParticipantAsync(conversationId, userId, cancellationToken);

        IQueryable<Message> messages = db.Messages.AsNoTracking().Where(m => m.ConversationId == conversationId);

        int totalCount = await messages.CountAsync(cancellationToken);
        return await messages
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.Id)
            .Select(ConversationQueries.ToMessageResponse(userId))
            .ToPagedResponseAsync(totalCount, query, cancellationToken);
    }

    public async Task<MessageResponse> SendMessageAsync(
        Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.Id;
        string content = OptionalText.Normalize(request.Content)
            ?? throw new RequestValidationException(nameof(SendMessageRequest.Content), "The message cannot be empty.");

        Conversation conversation = await db.Conversations
            .WithParticipant(userId)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException(ConversationNotFound);

        DateTimeOffset now = timeProvider.GetUtcNow();
        var message = new Message { ConversationId = conversation.Id, SenderId = userId, Content = content, SentAt = now };
        db.Messages.Add(message);
        conversation.LastMessageAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return new MessageResponse(message.Id, IsMine: true, message.Content, message.SentAt, ReadAt: null);
    }

    /// <summary>Marks the other participant's unread messages as read. Calling it again changes nothing.</summary>
    public async Task MarkAsReadAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.Id;
        await EnsureParticipantAsync(conversationId, userId, cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await db.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && m.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.ReadAt, now), cancellationToken);
    }

    private Task<Guid?> FindStartedByAsync(Guid postId, Guid userId, CancellationToken cancellationToken) =>
        db.Conversations
            .AsNoTracking()
            .Where(c => c.PostId == postId && c.StarterId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<ConversationResponse> LoadAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        await db.Conversations
            .AsNoTracking()
            .WithParticipant(userId)
            .Where(c => c.Id == id)
            .Select(ConversationQueries.ToResponse(userId))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(ConversationNotFound);

    private async Task EnsureParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        bool isParticipant = await db.Conversations
            .WithParticipant(userId)
            .AnyAsync(c => c.Id == conversationId, cancellationToken);

        if (!isParticipant)
        {
            throw new NotFoundException(ConversationNotFound);
        }
    }
}
