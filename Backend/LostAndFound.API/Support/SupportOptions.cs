using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Support;

/// <summary>Bound from the "Support" section.</summary>
public sealed class SupportOptions
{
    public const string SectionName = "Support";

    /// <summary>Where a copy of every support ticket is emailed, for example the support team's shared inbox.</summary>
    [Required, EmailAddress]
    public string InboxEmail { get; init; } = string.Empty;
}
