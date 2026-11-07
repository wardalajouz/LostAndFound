using LostAndFound.Api.Contracts.Support;

namespace LostAndFound.Api.Services;

public sealed class SupportTicketService : ISupportTicketService
{
    public Task CreateAsync(CreateSupportTicketRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
