using System.Net;
using System.Net.Http.Json;
using LostAndFound.Api.Email;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Support;

public sealed class SupportTicketTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private const string SupportInbox = "support@findly.local";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Guest_ticket_is_saved_and_emailed_to_support_with_reply_to_the_sender()
    {
        string subject = $"Cannot upload {PostApi.Marker()}";

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/support-tickets", Ticket(subject, email: "guest@findly.test"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await factory.WithDbAsync(async db =>
        {
            var ticket = await db.SupportTickets.SingleAsync(t => t.Subject == subject);
            Assert.Null(ticket.UserId);
            Assert.Equal("guest@findly.test", ticket.Email);
        });
        EmailMessage email = SentAbout(subject);
        Assert.Equal($"[Findly support] {subject}", email.Subject);
        Assert.Equal("guest@findly.test", email.ReplyToAddress);
        Assert.Contains("The upload button does nothing on my phone.", email.TextBody);
        Assert.Contains("guest (not signed in)", email.TextBody);
    }

    [Fact]
    public async Task Signed_in_ticket_is_linked_to_the_account()
    {
        (string token, _, Guid id) = await _client.NewAccountAsync();
        string subject = $"Account question {PostApi.Marker()}";

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, "/api/support-tickets", token, Ticket(subject));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(id, (await db.SupportTickets.SingleAsync(t => t.Subject == subject)).UserId));
        Assert.Contains(id.ToString(), SentAbout(subject).TextBody);
    }

    [Fact]
    public async Task Line_breaks_in_the_subject_or_name_cannot_add_email_headers()
    {
        string marker = PostApi.Marker();

        await _client.PostAsJsonAsync("/api/support-tickets", Ticket($"Help {marker}\r\nBcc: attacker@evil.test", fullName: "Ward\nBcc: x@evil.test"));

        EmailMessage email = factory.Emails.SentTo(SupportInbox).Single(m => m.Subject.Contains(marker, StringComparison.Ordinal));
        Assert.DoesNotContain('\n', email.Subject);
        Assert.DoesNotContain('\r', email.Subject);
        Assert.Equal($"[Findly support] Help {marker}  Bcc: attacker@evil.test", email.Subject);
        Assert.StartsWith("From: Ward Bcc: x@evil.test <", email.TextBody.Split('\n')[0]);
    }

    [Fact]
    public async Task Ticket_is_kept_even_when_the_email_cannot_be_sent()
    {
        string subject = $"Email down {PostApi.Marker()}";
        factory.Emails.FailNextSend();

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/support-tickets", Ticket(subject));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await factory.WithDbAsync(async db => Assert.True(await db.SupportTickets.AnyAsync(t => t.Subject == subject)));
    }

    [Theory]
    [InlineData("W", "ward@findly.test", "Help me", "Long enough message.")]
    [InlineData("Ward", "not-an-email", "Help me", "Long enough message.")]
    [InlineData("Ward", "ward@findly.test", "Hi", "Long enough message.")]
    [InlineData("Ward", "ward@findly.test", "Help me", "Too short")]
    public async Task Invalid_tickets_are_rejected_and_not_saved(string fullName, string email, string subject, string message)
    {
        int before = 0;
        await factory.WithDbAsync(async db => before = await db.SupportTickets.CountAsync());

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/support-tickets", new { fullName, email, subject, message });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(before, await db.SupportTickets.CountAsync()));
    }

    private static object Ticket(string subject, string fullName = "Ward Alajouz", string email = "ward@findly.test") => new
    {
        fullName,
        email,
        subject,
        message = "The upload button does nothing on my phone."
    };

    private EmailMessage SentAbout(string subject) =>
        factory.Emails.SentTo(SupportInbox).Single(m => m.Subject.EndsWith(subject, StringComparison.Ordinal));
}
