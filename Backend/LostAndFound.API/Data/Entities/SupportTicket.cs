namespace LostAndFound.Api.Data.Entities;

public sealed class SupportTicket
{
    public Guid Id { get; set; }

    /// <summary>Null when a guest submitted the form.</summary>
    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public required string FullName { get; set; }

    public required string Email { get; set; }

    public required string Subject { get; set; }

    public required string Message { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
