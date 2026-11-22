using System.Collections.Concurrent;
using LostAndFound.Api.Email;

namespace LostAndFound.Api.Tests.Infrastructure;

public sealed class RecordingEmailService : IEmailService
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();
    private int _failNext;

    public IReadOnlyCollection<EmailMessage> Sent => _sent;

    /// <summary>Makes the next send throw, like an email provider that is down.</summary>
    public void FailNextSend() => Interlocked.Exchange(ref _failNext, 1);

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _failNext, 0) == 1)
        {
            throw new IOException("Simulated email provider failure.");
        }

        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> SentTo(string address) =>
        _sent.Where(m => string.Equals(m.ToAddress, address, StringComparison.OrdinalIgnoreCase)).ToList();
}
