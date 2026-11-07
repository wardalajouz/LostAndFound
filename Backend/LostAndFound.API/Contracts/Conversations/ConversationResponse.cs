using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Conversations;

public sealed record ConversationResponse(
    Guid Id,
    PostReferenceResponse Post,
    UserSummaryResponse OtherParticipant,
    MessageResponse? LastMessage,
    int UnreadCount,
    DateTimeOffset CreatedAt);
