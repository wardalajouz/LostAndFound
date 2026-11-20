using System.Linq.Expressions;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Services.Posts;

namespace LostAndFound.Api.Services.Conversations;

/// <summary>
/// Query building blocks for chats. A conversation has exactly two participants:
/// the user who started it and the owner of the post it is about.
/// </summary>
internal static class ConversationQueries
{
    /// <summary>
    /// Conversations the user takes part in. Both sides come from stored rows (the starter and
    /// the post's owner), compared with the signed-in user's id from the verified token.
    /// </summary>
    public static IQueryable<Conversation> WithParticipant(this IQueryable<Conversation> conversations, Guid userId) =>
        conversations.Where(c => c.StarterId == userId || c.Post.OwnerId == userId);

    /// <summary>
    /// The inbox hides a chat from the post owner until the first message arrives,
    /// so pressing "Message Owner" without writing anything does not clutter their inbox.
    /// </summary>
    public static IQueryable<Conversation> VisibleInInboxOf(this IQueryable<Conversation> conversations, Guid userId) =>
        conversations.WithParticipant(userId).Where(c => c.StarterId == userId || c.LastMessageAt != null);

    /// <summary>Most recent activity first. Id breaks ties so paging is stable.</summary>
    public static IQueryable<Conversation> ByLatestActivity(this IQueryable<Conversation> conversations) =>
        conversations
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ThenByDescending(c => c.Id);

    public static Expression<Func<Conversation, ConversationResponse>> ToResponse(Guid userId) =>
        c => new ConversationResponse(
            c.Id,
            new PostReferenceResponse(
                c.Post.Id,
                c.Post.Type,
                c.Post.ItemName,
                c.Post.Images.OrderBy(image => image.SortOrder).Select(image => ImageUrls.PathPrefix + image.StorageKey).FirstOrDefault()),
            new UserSummaryResponse(
                c.StarterId == userId ? c.Post.OwnerId : c.StarterId,
                c.StarterId == userId ? c.Post.Owner.FullName : c.Starter.FullName,
                c.StarterId == userId ? c.Post.Owner.AvatarUrl : c.Starter.AvatarUrl),
            c.Messages
                .OrderByDescending(m => m.SentAt)
                .ThenByDescending(m => m.Id)
                .Select(m => new MessageResponse(m.Id, m.SenderId == userId, m.Content, m.SentAt, m.ReadAt))
                .FirstOrDefault(),
            c.Messages.Count(m => m.SenderId != userId && m.ReadAt == null),
            c.CreatedAt);

    public static Expression<Func<Message, MessageResponse>> ToMessageResponse(Guid userId) =>
        m => new MessageResponse(m.Id, m.SenderId == userId, m.Content, m.SentAt, m.ReadAt);
}
