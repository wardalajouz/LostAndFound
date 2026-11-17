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

public sealed class PostService(
    AppDbContext db,
    ICurrentUser currentUser,
    IRequestLanguage requestLanguage,
    TimeProvider timeProvider) : IPostService
{
    private const string PostNotFound = "Post not found.";

    /// <summary>Public browse: only visible posts that are still active.</summary>
    public async Task<PagedResponse<PostSummaryResponse>> BrowseAsync(BrowsePostsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Post> posts = db.Posts
            .AsNoTracking()
            .Where(post => post.Visibility == PostVisibility.Visible && post.Status == PostStatus.Active)
            .MatchingSearch(query.Search);

        if (query.Type is { } type)
        {
            posts = posts.Where(post => post.Type == type);
        }

        if (query.Category is { } category)
        {
            posts = posts.Where(post => post.Category == category);
        }

        if (query.CampusLocationId is { } campusLocationId)
        {
            posts = posts.Where(post => post.CampusLocationId == campusLocationId);
        }

        if (query.Date is { } date)
        {
            posts = posts.Where(post => post.DateOccurred == date);
        }

        int totalCount = await posts.CountAsync(cancellationToken);
        return await posts
            .SortedBy(query.Sort)
            .Select(PostProjections.ToSummary(requestLanguage.Current))
            .ToPagedResponseAsync(totalCount, query, cancellationToken);
    }

    /// <summary>Public details. Resolved posts stay reachable so shared links keep working; hidden and deleted ones do not.</summary>
    public async Task<PostResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Posts
            .AsNoTracking()
            .Where(post => post.Id == id && post.Visibility == PostVisibility.Visible)
            .Select(PostProjections.ToResponse(requestLanguage.Current))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(PostNotFound);

    public async Task<PostResponse> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken)
    {
        await EnsureLocationAvailableAsync(request.CampusLocationId, cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        var post = new Post
        {
            OwnerId = currentUser.Id,
            Type = request.Type,
            ItemName = string.Empty,
            Color = string.Empty,
            Description = string.Empty,
            CreatedAt = now
        };
        ApplyDetails(post, request, now);

        db.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);

        return await LoadResponseAsync(post.Id, cancellationToken);
    }

    public async Task<PostResponse> UpdateAsync(Guid id, UpdatePostRequest request, CancellationToken cancellationToken)
    {
        Post post = await FindOwnPostAsync(id, cancellationToken);

        if (post.Status == PostStatus.Resolved)
        {
            throw new ConflictException("Resolved posts cannot be edited.");
        }

        if (post.Type == PostType.Lost && (request.HandedToSecurity || OptionalText.Normalize(request.VerificationDetails) is not null))
        {
            const string error = "Only found posts can be handed to security or have verification details.";
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                [nameof(UpdatePostRequest.HandedToSecurity)] = [error],
                [nameof(UpdatePostRequest.VerificationDetails)] = [error]
            });
        }

        if (request.CampusLocationId != post.CampusLocationId)
        {
            await EnsureLocationAvailableAsync(request.CampusLocationId, cancellationToken);
        }

        ApplyDetails(post, request, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return await LoadResponseAsync(post.Id, cancellationToken);
    }

    /// <summary>Soft delete: the row stays so conversations about the post keep working.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Post post = await FindOwnPostAsync(id, cancellationToken);

        post.Visibility = PostVisibility.Deleted;
        post.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Idempotent: resolving an already resolved post changes nothing.</summary>
    public async Task ResolveAsync(Guid id, CancellationToken cancellationToken)
    {
        Post post = await FindOwnPostAsync(id, cancellationToken);
        if (post.Status == PostStatus.Resolved)
        {
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        post.Status = PostStatus.Resolved;
        post.ResolvedAt = now;
        post.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// The owner comes from the stored post and the caller from the verified token.
    /// Someone else's post gets the same 404 as a missing one.
    /// </summary>
    private async Task<Post> FindOwnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.Id;
        return await db.Posts.FirstOrDefaultAsync(
                post => post.Id == id && post.OwnerId == userId && post.Visibility != PostVisibility.Deleted,
                cancellationToken)
            ?? throw new NotFoundException(PostNotFound);
    }

    private async Task EnsureLocationAvailableAsync(int campusLocationId, CancellationToken cancellationToken)
    {
        bool available = await db.CampusLocations.AnyAsync(
            location => location.Id == campusLocationId && location.IsActive, cancellationToken);

        if (!available)
        {
            throw new RequestValidationException(nameof(PostDetails.CampusLocationId), "Choose a campus location from the list.");
        }
    }

    private async Task<PostResponse> LoadResponseAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Posts
            .AsNoTracking()
            .Where(post => post.Id == id)
            .Select(PostProjections.ToResponse(requestLanguage.Current))
            .FirstAsync(cancellationToken);

    /// <summary>Copies the editable fields. Found-only fields are cleared on lost posts.</summary>
    private static void ApplyDetails(Post post, PostDetails details, DateTimeOffset now)
    {
        bool found = post.Type == PostType.Found;

        post.ItemName = details.ItemName.Trim();
        post.Category = details.Category;
        post.Color = details.Color.Trim();
        post.Brand = OptionalText.Normalize(details.Brand);
        post.Description = details.Description.Trim();
        post.CampusLocationId = details.CampusLocationId;
        post.AdditionalLocationDetails = OptionalText.Normalize(details.AdditionalLocationDetails);
        post.DateOccurred = details.DateOccurred;
        post.HandedToSecurity = found && details.HandedToSecurity;
        post.VerificationDetails = found ? OptionalText.Normalize(details.VerificationDetails) : null;
        post.UpdatedAt = now;
    }
}
