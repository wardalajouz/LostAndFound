using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.CampusLocations;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Localization;
using LostAndFound.Api.Logging;
using LostAndFound.Api.Notifications;
using LostAndFound.Api.Services.Admin;
using LostAndFound.Api.Services.Posts;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

/// <summary>
/// The admin posts table. Unlike the public pages it includes hidden and deleted posts,
/// so administrators can review and restore them. Every visibility change is logged with its reason.
/// </summary>
public sealed class AdminPostService(
    AppDbContext db,
    ICurrentUser currentUser,
    IRequestLanguage requestLanguage,
    INotifier notifier,
    TimeProvider timeProvider,
    ILogger<AdminPostService> logger) : IAdminPostService
{
    private const string PostNotFound = "Post not found.";

    public async Task<PagedResponse<AdminPostResponse>> GetAsync(AdminPostsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Post> posts = db.Posts.AsNoTracking().MatchingSearch(query.Search);

        if (query.Type is { } type)
        {
            posts = posts.Where(p => p.Type == type);
        }

        if (query.Category is { } category)
        {
            posts = posts.Where(p => p.Category == category);
        }

        if (query.Status is { } status)
        {
            posts = posts.Where(p => p.Status == status);
        }

        if (query.CampusLocationId is { } campusLocationId)
        {
            posts = posts.Where(p => p.CampusLocationId == campusLocationId);
        }

        if (query.HandedToSecurity is { } handedToSecurity)
        {
            posts = posts.Where(p => p.HandedToSecurity == handedToSecurity);
        }

        if (query.Visibility is { } visibility)
        {
            posts = posts.Where(p => p.Visibility == visibility);
        }

        bool arabic = requestLanguage.Current == PreferredLanguage.Arabic;
        int totalCount = await posts.CountAsync(cancellationToken);

        return await posts
            .SortedBy(query.Sort)
            .Select(p => new AdminPostResponse(
                p.Id,
                p.Type,
                p.ItemName,
                p.Category,
                p.Status,
                p.Visibility,
                p.HandedToSecurity,
                new CampusLocationResponse(p.CampusLocation.Id, arabic ? p.CampusLocation.NameAr : p.CampusLocation.NameEn),
                p.DateOccurred,
                new UserSummaryResponse(p.Owner.Id, p.Owner.FullName, p.Owner.AvatarUrl),
                p.CreatedAt,
                p.ResolvedAt))
            .ToPagedResponseAsync(totalCount, query, cancellationToken);
    }

    /// <summary>Includes the verification details, so security staff can check who an item belongs to.</summary>
    public async Task<AdminPostDetailsResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        IQueryable<Post> post = db.Posts.AsNoTracking().Where(p => p.Id == id);

        PostResponse details = await post
            .Select(PostProjections.ToResponse(requestLanguage.Current))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(PostNotFound);

        var moderation = await post
            .Select(p => new { p.Visibility, p.VerificationDetails, p.ResolvedAt })
            .FirstAsync(cancellationToken);

        List<ModerationLogEntryResponse> history = await db.PostModerationLogs
            .AsNoTracking()
            .Where(log => log.PostId == id)
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Select(log => new ModerationLogEntryResponse(
                log.Visibility,
                log.Reason,
                new UserSummaryResponse(log.Admin.Id, log.Admin.FullName, log.Admin.AvatarUrl),
                log.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AdminPostDetailsResponse(details, moderation.Visibility, moderation.VerificationDetails, moderation.ResolvedAt, history);
    }

    public async Task UpdateVisibilityAsync(Guid id, UpdatePostVisibilityRequest request, CancellationToken cancellationToken)
    {
        string reason = ModerationReason.Require(request.Reason, nameof(UpdatePostVisibilityRequest.Reason));

        var target = await db.Posts
            .Where(p => p.Id == id)
            .Select(p => new { Post = p, OwnerDeleted = p.Owner.DeletedAt != null })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(PostNotFound);

        Post post = target.Post;
        if (post.Visibility == request.Visibility)
        {
            throw new ConflictException($"The post is already {request.Visibility.ToString().ToLowerInvariant()}.");
        }

        if (target.OwnerDeleted && request.Visibility != PostVisibility.Deleted)
        {
            throw new ConflictException("The owner deleted their account, so this post cannot be shown again.");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        post.Visibility = request.Visibility;
        post.UpdatedAt = now;
        db.PostModerationLogs.Add(new PostModerationLog
        {
            PostId = post.Id,
            AdminId = currentUser.Id,
            Visibility = request.Visibility,
            Reason = reason,
            CreatedAt = now
        });
        await notifier.NotifyPostModeratedAsync(post.Id, post.OwnerId, now, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        logger.PostModerated(currentUser.Id, post.Id, request.Visibility);
    }
}
