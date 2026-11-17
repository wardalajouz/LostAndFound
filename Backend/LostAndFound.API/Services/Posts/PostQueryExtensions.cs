using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services.Posts;

/// <summary>Search, sort, and paging shared by the browse page and "My Items".</summary>
internal static class PostQueryExtensions
{
    private const char LikeEscape = '\\';

    /// <summary>Case-insensitive match on item name, description, brand, or color. The text is matched literally.</summary>
    public static IQueryable<Post> MatchingSearch(this IQueryable<Post> posts, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return posts;
        }

        string pattern = $"%{EscapeLike(search.Trim())}%";
        return posts.Where(post =>
            EF.Functions.Like(post.ItemName, pattern, LikeEscape.ToString())
            || EF.Functions.Like(post.Description, pattern, LikeEscape.ToString())
            || (post.Brand != null && EF.Functions.Like(post.Brand, pattern, LikeEscape.ToString()))
            || EF.Functions.Like(post.Color, pattern, LikeEscape.ToString()));
    }

    /// <summary>Id is a tie-breaker, so paging is stable when two posts share a timestamp.</summary>
    public static IQueryable<Post> SortedBy(this IQueryable<Post> posts, PostSort sort) => sort == PostSort.Oldest
        ? posts.OrderBy(post => post.CreatedAt).ThenBy(post => post.Id)
        : posts.OrderByDescending(post => post.CreatedAt).ThenByDescending(post => post.Id);

    public static async Task<PagedResponse<TResult>> ToPagedResponseAsync<TResult>(
        this IQueryable<TResult> sortedQuery, int totalCount, PagedQuery paging, CancellationToken cancellationToken)
    {
        List<TResult> items = await sortedQuery
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TResult>(items, paging.Page, paging.PageSize, totalCount);
    }

    private static string EscapeLike(string text) => text
        .Replace(@"\", @"\\", StringComparison.Ordinal)
        .Replace("%", @"\%", StringComparison.Ordinal)
        .Replace("_", @"\_", StringComparison.Ordinal)
        .Replace("[", @"\[", StringComparison.Ordinal);
}
