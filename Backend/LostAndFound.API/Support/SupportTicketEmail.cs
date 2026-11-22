using System.Text;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Email;

namespace LostAndFound.Api.Support;

/// <summary>The copy of a support ticket sent to the support inbox. Replying answers the person who wrote it.</summary>
public static class SupportTicketEmail
{
    public static EmailMessage Create(SupportTicket ticket, string inboxEmail) => new(
        inboxEmail,
        "Findly Support",
        $"[Findly support] {SingleLine(ticket.Subject)}",
        $"""
        From: {SingleLine(ticket.FullName)} <{ticket.Email}>
        Account: {(ticket.UserId is { } userId ? userId.ToString() : "guest (not signed in)")}
        Ticket: {ticket.Id}
        Sent: {ticket.CreatedAt:yyyy-MM-dd HH:mm} UTC

        {ticket.Message}
        """,
        ReplyToAddress: ticket.Email);

    /// <summary>Line breaks and other control characters become spaces, so user text cannot add email header lines.</summary>
    public static string SingleLine(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            builder.Append(char.IsControl(c) ? ' ' : c);
        }

        return builder.ToString().Trim();
    }
}
