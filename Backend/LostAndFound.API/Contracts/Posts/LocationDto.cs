using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Posts;

public sealed record LocationDto
{
    [Range(-90, 90)]
    public required double Latitude { get; init; }

    [Range(-180, 180)]
    public required double Longitude { get; init; }

    [StringLength(300)]
    public string? Description { get; init; }
}

