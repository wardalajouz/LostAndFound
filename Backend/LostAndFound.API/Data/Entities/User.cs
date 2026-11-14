using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Contracts.Users;
using Microsoft.AspNetCore.Identity;

namespace LostAndFound.Api.Data.Entities;

/// <summary>An account. Email, password hash, and lockout come from ASP.NET Core Identity.</summary>
public sealed class User : IdentityUser<Guid>
{
    public required string FullName { get; set; }

    public UserType UserType { get; set; }

    public string? AvatarUrl { get; set; }

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public PreferredLanguage PreferredLanguage { get; set; } = PreferredLanguage.Arabic;

    public bool NotifyOnNewMessage { get; set; } = true;

    public bool NotifyOnSimilarItem { get; set; } = true;

    public bool NotifyOnPostUpdate { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Set when the account is deleted. Personal data is cleared but the row is kept.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<Post> Posts { get; } = [];
}
