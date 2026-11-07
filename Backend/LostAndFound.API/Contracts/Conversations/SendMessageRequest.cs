using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Conversations;

public sealed record SendMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public required string Content { get; init; }
}
