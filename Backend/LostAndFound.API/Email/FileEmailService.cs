namespace LostAndFound.Api.Email;

/// <summary>
/// Development only: writes each email to App_Data/emails as a text file instead of sending it,
/// so password reset links can be tested without an email provider.
/// </summary>
public sealed class FileEmailService(IWebHostEnvironment environment, TimeProvider timeProvider, ILogger<FileEmailService> logger)
    : IEmailService
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        string folder = Path.Combine(environment.ContentRootPath, "App_Data", "emails");
        Directory.CreateDirectory(folder);

        string fileName = $"{timeProvider.GetUtcNow():yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(folder, fileName);

        string replyTo = message.ReplyToAddress is null ? string.Empty : $"Reply-To: {message.ReplyToAddress}\n";
        string content = $"To: {message.ToName} <{message.ToAddress}>\n{replyTo}Subject: {message.Subject}\n\n{message.TextBody}\n";
        await File.WriteAllTextAsync(path, content, cancellationToken);

        logger.LogInformation("Development email written to {Path}.", path);
    }
}
