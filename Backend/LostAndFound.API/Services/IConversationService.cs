using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Conversations;

namespace LostAndFound.Api.Services;

public interface IConversationService
{
    /// <summary>Returns the existing conversation for this post and user, or creates one.</summary>
    Task<ConversationResponse> StartAsync(StartConversationRequest request, CancellationToken cancellationToken);

    Task<PagedResponse<ConversationResponse>> GetAllAsync(PagedQuery query, CancellationToken cancellationToken);

    Task<ConversationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResponse<MessageResponse>> GetMessagesAsync(
        Guid conversationId, PagedQuery query, CancellationToken cancellationToken);

    Task<MessageResponse> SendMessageAsync(
        Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken);

    Task MarkAsReadAsync(Guid conversationId, CancellationToken cancellationToken);
}
