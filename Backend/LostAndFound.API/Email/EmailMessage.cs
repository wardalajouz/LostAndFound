namespace LostAndFound.Api.Email;

public sealed record EmailMessage(string ToAddress, string ToName, string Subject, string TextBody);
