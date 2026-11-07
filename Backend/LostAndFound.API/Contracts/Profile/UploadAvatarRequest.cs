using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Profile;

public sealed record UploadAvatarRequest
{
    /// <summary>File type and size are checked by the service.</summary>
    [Required]
    public required IFormFile Image { get; init; }
}
