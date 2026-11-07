namespace LostAndFound.Api.Contracts.Conversations;

public sealed record MessageResponse(
    Guid Id,
    bool IsMine,
    string Content,
    DateTimeOffset SentAt,
    DateTimeOffset? ReadAt);
