using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LostAndFound.Api.Email;

public sealed class SmtpEmailService(IOptions<EmailOptions> options) : IEmailService
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        EmailOptions email = options.Value;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(email.FromName, email.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("plain") { Text = message.TextBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(email.Smtp.Host, email.Smtp.Port, SecureSocketOptions.StartTls, cancellationToken);

        if (!string.IsNullOrEmpty(email.Smtp.Username))
        {
            await client.AuthenticateAsync(email.Smtp.Username, email.Smtp.Password, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
