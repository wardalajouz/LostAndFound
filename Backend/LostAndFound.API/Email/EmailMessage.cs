namespace LostAndFound.Api.Email;

/// <param name="ReplyToAddress">Where replies go, if not the sender, for example the person who wrote a support ticket.</param>
public sealed record EmailMessage(string ToAddress, string ToName, string Subject, string TextBody, string? ReplyToAddress = null);
