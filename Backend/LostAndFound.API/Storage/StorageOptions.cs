using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Storage;

/// <summary>Bound from the "Storage" section.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Folder for uploaded files. A relative path is resolved from the app's content root.</summary>
    [Required]
    public string RootPath { get; init; } = Path.Combine("App_Data", "uploads");
}
