using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Posts;

public sealed record UploadPostImagesRequest
{
    /// <summary>1 to 5 images. File type and size are checked by the service.</summary>
    [Required, MinLength(1), MaxLength(5)]
    public required IReadOnlyList<IFormFile> Images { get; init; }
}
