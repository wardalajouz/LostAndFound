using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Data.Entities;

public sealed class UserModerationLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid AdminId { get; set; }

    public User Admin { get; set; } = null!;

    /// <summary>The status the admin set.</summary>
    public AccountStatus Status { get; set; }

    public required string Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
