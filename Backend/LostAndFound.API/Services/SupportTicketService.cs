using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Support;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Email;
using LostAndFound.Api.Support;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Services;

/// <summary>Stores Help &amp; Support messages from guests and signed-in users, and emails a copy to the support inbox.</summary>
public sealed class SupportTicketService(
    AppDbContext db,
    ICurrentUser currentUser,
    IEmailService emailService,
    IOptions<SupportOptions> supportOptions,
    TimeProvider timeProvider,
    ILogger<SupportTicketService> logger) : ISupportTicketService
{
    public async Task CreateAsync(CreateSupportTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = new SupportTicket
        {
            UserId = currentUser.OptionalId,
            FullName = SupportTicketEmail.SingleLine(request.FullName),
            Email = request.Email.Trim(),
            Subject = SupportTicketEmail.SingleLine(request.Subject),
            Message = request.Message.Trim(),
            CreatedAt = timeProvider.GetUtcNow()
        };

        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendAsync(SupportTicketEmail.Create(ticket, supportOptions.Value.InboxEmail), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The ticket is already saved, so the user's message is not lost; the copy can be found in the database.
            logger.LogError(exception, "Could not email support ticket {TicketId} to the support inbox.", ticket.Id);
        }
    }
}
