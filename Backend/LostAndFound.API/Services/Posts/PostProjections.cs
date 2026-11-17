using System.Linq.Expressions;
using LostAndFound.Api.Contracts.CampusLocations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data.Entities;

namespace LostAndFound.Api.Services.Posts;

/// <summary>
/// Database projections from posts to API responses. EF Core translates them to SQL,
/// so only the needed columns are read. None of them include VerificationDetails.
/// </summary>
internal static class PostProjections
{
    public const int ShortDescriptionLength = 150;

    public static Expression<Func<Post, PostResponse>> ToResponse(PreferredLanguage language)
    {
        bool arabic = language == PreferredLanguage.Arabic;
        return post => new PostResponse(
            post.Id,
            post.Type,
            post.ItemName,
            post.Category,
            post.Color,
            post.Brand,
            post.Description,
            new CampusLocationResponse(post.CampusLocation.Id, arabic ? post.CampusLocation.NameAr : post.CampusLocation.NameEn),
            post.AdditionalLocationDetails,
            post.DateOccurred,
            post.HandedToSecurity,
            post.Status,
            post.Images
                .OrderBy(image => image.SortOrder)
                .Select(image => new PostImageResponse(image.Id, ImageUrls.PathPrefix + image.StorageKey))
                .ToList(),
            new UserSummaryResponse(post.Owner.Id, post.Owner.FullName, post.Owner.AvatarUrl),
            post.CreatedAt);
    }

    public static Expression<Func<Post, PostSummaryResponse>> ToSummary(PreferredLanguage language)
    {
        bool arabic = language == PreferredLanguage.Arabic;
        return post => new PostSummaryResponse(
            post.Id,
            post.Type,
            post.ItemName,
            post.Category,
            new CampusLocationResponse(post.CampusLocation.Id, arabic ? post.CampusLocation.NameAr : post.CampusLocation.NameEn),
            post.DateOccurred,
            post.Status,
            post.Description.Length > ShortDescriptionLength
                ? post.Description.Substring(0, ShortDescriptionLength) + "…"
                : post.Description,
            post.Images
                .OrderBy(image => image.SortOrder)
                .Select(image => ImageUrls.PathPrefix + image.StorageKey)
                .FirstOrDefault());
    }
}
