using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Conversations;

namespace LostAndFound.Api.Services;

public sealed class ConversationService : IConversationService
{
    public Task<ConversationResponse> StartAsync(StartConversationRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<PagedResponse<ConversationResponse>> GetAllAsync(PagedQuery query, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<ConversationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<PagedResponse<MessageResponse>> GetMessagesAsync(
        Guid conversationId, PagedQuery query, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<MessageResponse> SendMessageAsync(
        Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task MarkAsReadAsync(Guid conversationId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
