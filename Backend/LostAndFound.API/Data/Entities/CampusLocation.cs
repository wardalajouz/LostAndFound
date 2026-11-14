namespace LostAndFound.Api.Data.Entities;

public sealed class CampusLocation
{
    public int Id { get; set; }

    public required string NameEn { get; set; }

    public required string NameAr { get; set; }

    /// <summary>Inactive locations are hidden from new posts but stay on existing ones.</summary>
    public bool IsActive { get; set; } = true;
}
