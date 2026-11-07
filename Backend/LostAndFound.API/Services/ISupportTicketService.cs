using LostAndFound.Api.Contracts.Support;

namespace LostAndFound.Api.Services;

public interface ISupportTicketService
{
    Task CreateAsync(CreateSupportTicketRequest request, CancellationToken cancellationToken);
}
