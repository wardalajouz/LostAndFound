namespace LostAndFound.Api.Contracts.Conversations;

public sealed record StartConversationRequest
{
    public required Guid PostId { get; init; }
}
