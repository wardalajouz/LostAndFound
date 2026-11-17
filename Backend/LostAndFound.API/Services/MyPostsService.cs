using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Localization;
using LostAndFound.Api.Services.Posts;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

/// <summary>The signed-in user's own posts, including resolved and admin-hidden ones. Deleted posts are excluded.</summary>
public sealed class MyPostsService(AppDbContext db, ICurrentUser currentUser, IRequestLanguage requestLanguage)
    : IMyPostsService
{
    public async Task<PagedResponse<PostSummaryResponse>> GetAsync(MyPostsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Post> posts = OwnPosts().MatchingSearch(query.Search);

        if (query.Type is { } type)
        {
            posts = posts.Where(post => post.Type == type);
        }

        if (query.Status is { } status)
        {
            posts = posts.Where(post => post.Status == status);
        }

        int totalCount = await posts.CountAsync(cancellationToken);
        return await posts
            .SortedBy(query.Sort)
            .Select(PostProjections.ToSummary(requestLanguage.Current))
            .ToPagedResponseAsync(totalCount, query, cancellationToken);
    }

    /// <summary>Includes the private verification details, because only the owner reaches this.</summary>
    public async Task<OwnPostResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        IQueryable<Post> post = OwnPosts().Where(p => p.Id == id);

        PostResponse response = await post
            .Select(PostProjections.ToResponse(requestLanguage.Current))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Post not found.");

        string? verificationDetails = await post.Select(p => p.VerificationDetails).FirstAsync(cancellationToken);
        return new OwnPostResponse(response, verificationDetails);
    }

    public async Task<PostStatsResponse> GetStatsAsync(CancellationToken cancellationToken)
    {
        var counts = await OwnPosts()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Lost = group.Count(post => post.Type == PostType.Lost),
                Found = group.Count(post => post.Type == PostType.Found),
                Active = group.Count(post => post.Status == PostStatus.Active),
                Resolved = group.Count(post => post.Status == PostStatus.Resolved)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return counts is null
            ? new PostStatsResponse(0, 0, 0, 0)
            : new PostStatsResponse(counts.Lost, counts.Found, counts.Active, counts.Resolved);
    }

    private IQueryable<Post> OwnPosts()
    {
        Guid userId = currentUser.Id;
        return db.Posts
            .AsNoTracking()
            .Where(post => post.OwnerId == userId && post.Visibility != PostVisibility.Deleted);
    }
}
